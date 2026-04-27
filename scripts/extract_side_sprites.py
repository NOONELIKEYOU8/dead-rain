import os
import sys
from PIL import Image

def extract_side_sprites(filepath, output_dir, rows, cols, target_row, direction_name):
    try:
        img = Image.open(filepath)
        width, height = img.size
        
        # Calculate cell dimensions
        cell_width = width // cols
        cell_height = height // rows
        
        # We only want the target_row
        frames = []
        for c in range(cols):
            left = c * cell_width
            top = target_row * cell_height
            right = left + cell_width
            bottom = top + cell_height
            
            frame = img.crop((left, top, right, bottom))
            frames.append(frame)
            
        # Combine all frames of this row horizontally
        out_img = Image.new('RGBA', (cell_width * cols, cell_height))
        for i, frame in enumerate(frames):
            out_img.paste(frame, (i * cell_width, 0))
            
        base_name = os.path.basename(filepath).replace(".png", "")
        out_filename = f"{base_name}_{direction_name}.png"
        out_path = os.path.join(output_dir, out_filename)
        
        out_img.save(out_path)
        print(f"Extracted {direction_name} frames from {filepath} to {out_path}")
        
    except Exception as e:
        print(f"Error processing {filepath}: {e}")

if __name__ == "__main__":
    # Example usage: python extract.py <input> <output_dir> <rows> <cols> <target_row> <direction_name>
    if len(sys.argv) < 7:
        print("Usage: python extract.py <filepath> <output_dir> <rows> <cols> <target_row> <direction>")
        sys.exit(1)
        
    filepath = sys.argv[1]
    out_dir = sys.argv[2]
    rows = int(sys.argv[3])
    cols = int(sys.argv[4])
    target_row = int(sys.argv[5])
    dir_name = sys.argv[6]
    
    if not os.path.exists(out_dir):
        os.makedirs(out_dir)
        
    extract_side_sprites(filepath, out_dir, rows, cols, target_row, dir_name)
