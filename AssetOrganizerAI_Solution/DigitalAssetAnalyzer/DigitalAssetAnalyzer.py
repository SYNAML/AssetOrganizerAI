#!/usr/bin/env python3

import argparse
import os
import sys
import json
import math
import concurrent.futures
from datetime import datetime
from pathlib import Path

import torch
from PIL import Image, UnidentifiedImageError
from transformers import (
    AutoImageProcessor, 
    AutoModelForImageClassification,
    VisionEncoderDecoderModel,
    ViTFeatureExtractor,
    AutoTokenizer
)

# ---------------
# CONFIGURATION
# ---------------
CLASSIFICATION_MODEL_NAME = "google/vit-base-patch16-224"
CAPTIONING_MODEL_NAME     = "nlpconnect/vit-gpt2-image-captioning"

# Initialize global models once to avoid reloading them for each image.
# We do this at the module-level or inside a function guarded by a global variable.
device = "cuda" if torch.cuda.is_available() else "cpu"

# Classification: ViT
classification_processor = AutoImageProcessor.from_pretrained(CLASSIFICATION_MODEL_NAME)
classification_model = AutoModelForImageClassification.from_pretrained(CLASSIFICATION_MODEL_NAME).to(device)
classification_model.eval()

# Captioning: ViT + GPT2
caption_feature_extractor = ViTFeatureExtractor.from_pretrained(CAPTIONING_MODEL_NAME)
caption_tokenizer = AutoTokenizer.from_pretrained(CAPTIONING_MODEL_NAME)
caption_model = VisionEncoderDecoderModel.from_pretrained(CAPTIONING_MODEL_NAME).to(device)
caption_model.eval()

# ---------------
# FUNCTIONS
# ---------------

def classify_image(image: Image.Image):
    """
    Classify the main content of the image using a ViT-based model.
    Returns top label with confidence or a list of top labels.
    """
    # Preprocess
    inputs = classification_processor(images=image, return_tensors="pt").to(device)
    
    with torch.no_grad():
        outputs = classification_model(**inputs)
    
    logits = outputs.logits
    predicted_probabilities = torch.softmax(logits, dim=-1)[0]
    predicted_label_idx = torch.argmax(predicted_probabilities).item()
    predicted_label = classification_model.config.id2label[predicted_label_idx]
    confidence = predicted_probabilities[predicted_label_idx].item()
    
    return predicted_label, confidence


def caption_image(image: Image.Image, max_length=16, num_beams=4):
    """
    Generate a caption for the image using a VisionEncoderDecoder (ViT -> GPT2).
    """
    inputs = caption_feature_extractor(images=image, return_tensors="pt").pixel_values.to(device)
    # Generate
    with torch.no_grad():
        output_ids = caption_model.generate(
            inputs, 
            max_length=max_length,
            num_beams=num_beams
        )
    # Decode
    preds = caption_tokenizer.batch_decode(output_ids, skip_special_tokens=True)
    caption = preds[0].strip()
    return caption


def detect_sprite_sheet(image: Image.Image):
    """
    Heuristic to guess if the image might be a sprite sheet.
    You can expand/replace this with a real detection approach.
    For now:
      - If the width is significantly larger than the height (or vice versa),
        and both are moderate (e.g., < 2048).
      - If the aspect ratio is a multiple of some typical frame sizes.
    """
    w, h = image.size
    # Simple ratio check:
    aspect_ratio = w / h if h != 0 else 0
    
    # Example naive heuristics:
    # If there's a repeating pattern or a grid-like dimension,
    # we'd expect integer multiples of smaller frames.
    # We'll do a simple guess by dividing width or height by common frame sizes.
    sprite_keywords = []

    # Let's guess that if width or height is an exact multiple of ~64 or ~128 or ~256
    # that might indicate a sprite sheet (commonly used in 2D games).
    def is_multiple_of_any(value, candidates, tolerance=2):
        # Tolerance-based check: value % c is less than e.g. 2
        return any(abs(value % c) <= tolerance for c in candidates)

    common_frame_sizes = [32, 64, 128, 256, 512]
    if is_multiple_of_any(w, common_frame_sizes) and is_multiple_of_any(h, common_frame_sizes):
        # Possibly a sprite-based dimension
        sprite_keywords.append("sprite-sheet-like-dimensions")
    
    # Another check: extremely wide or tall images might be a sprite strip.
    # For example, width >> height or vice versa.
    if aspect_ratio > 5 or aspect_ratio < 0.2:
        sprite_keywords.append("possible-sprite-strip")
    
    return sprite_keywords


def analyze_image(image_path: Path):
    """
    Loads an image, runs classification + captioning, 
    collects metadata, and returns a dictionary of results.
    """
    result = {
        "file_path": str(image_path),
        "classification_label": None,
        "classification_confidence": None,
        "caption": None,
        "width": None,
        "height": None,
        "sprite_sheet_heuristics": [],
        "metadata": {},
    }

    try:
        with Image.open(image_path) as img:
            img = img.convert("RGB")  # Ensure 3-channel color
            w, h = img.size
            result["width"] = w
            result["height"] = h

            # Classification
            label, conf = classify_image(img)
            result["classification_label"] = label
            result["classification_confidence"] = float(conf)

            # Captioning
            cap = caption_image(img)
            result["caption"] = cap

            # Sprite sheet detection
            sprite_clues = detect_sprite_sheet(img)
            result["sprite_sheet_heuristics"] = sprite_clues
        
        # Gather basic file metadata
        stat = image_path.stat()
        result["metadata"] = {
            "file_size_bytes": stat.st_size,
            "created": datetime.fromtimestamp(stat.st_ctime).isoformat(),
            "modified": datetime.fromtimestamp(stat.st_mtime).isoformat(),
        }

    except UnidentifiedImageError:
        result["error"] = "Unidentified image file (cannot open)."
    except Exception as e:
        result["error"] = f"{type(e).__name__}: {e}"

    return result


def get_image_paths(root_dir: Path):
    """
    Recursively find all files in 'root_dir' that have common image extensions.
    """
    exts = {".png", ".jpg", ".jpeg", ".gif", ".bmp", ".tiff", ".webp"}
    return [p for p in root_dir.rglob("*") if p.suffix.lower() in exts]


def main():
    parser = argparse.ArgumentParser(
        description="Analyze images with local Transformers classification & captioning, output JSON."
    )
    parser.add_argument(
        "directory",
        type=str,
        help="Directory containing images to analyze."
    )
    parser.add_argument(
        "--output",
        type=str,
        default="",
        help="Output JSON file path. If omitted, prints to stdout."
    )
    parser.add_argument(
        "--max-workers",
        type=int,
        default=4,
        help="Number of worker threads or processes for parallelism."
    )
    args = parser.parse_args()

    root_dir = Path(args.directory)
    if not root_dir.exists() or not root_dir.is_dir():
        print(f"ERROR: Directory not found: {root_dir}", file=sys.stderr)
        sys.exit(1)
    
    image_paths = get_image_paths(root_dir)
    if not image_paths:
        print("No images found in the specified directory.")
        sys.exit(0)

    print(f"Found {len(image_paths)} images. Analyzing in parallel...", file=sys.stderr)
    
    # Parallel processing
    # You could use ProcessPoolExecutor for CPU-bound tasks, but we have GPU-based tasks here.
    # Typically, for GPU tasks, you might keep it single-threaded or carefully manage concurrency 
    # if you have multiple GPUs. We'll still show concurrency as an example.
    results = []
    with concurrent.futures.ThreadPoolExecutor(max_workers=args.max_workers) as executor:
        future_to_path = {executor.submit(analyze_image, p): p for p in image_paths}
        for future in concurrent.futures.as_completed(future_to_path):
            res = future.result()
            results.append(res)

    # Sort results by file path for readability
    results.sort(key=lambda x: x["file_path"])

    # Output JSON (to file or stdout)
    output_data = json.dumps(results, indent=2)
    if args.output:
        with open(args.output, "w", encoding="utf-8") as f:
            f.write(output_data)
        print(f"Analysis complete. Results written to: {args.output}")
    else:
        print(output_data)


if __name__ == "__main__":
    main()
