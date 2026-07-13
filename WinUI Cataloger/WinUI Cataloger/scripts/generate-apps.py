import os
import re
import json
import urllib.request
import urllib.parse
# pyrefly: ignore [missing-import]
from PIL import Image

# Constants
SCRIPT_DIR = os.path.dirname(os.path.abspath(__file__))
PROJECT_DIR = os.path.dirname(SCRIPT_DIR)
README_PATH = os.path.abspath(os.path.join(PROJECT_DIR, '..', '..', 'README.md'))
OUTPUT_JSON_PATH = os.path.join(PROJECT_DIR, 'Assets', 'data', 'apps-data.json')
LOGO_DIR = os.path.join(PROJECT_DIR, 'Assets', 'apps')

def ensure_dir(d):
    if not os.path.exists(d):
        os.makedirs(d, exist_ok=True)

def slugify(text):
    text = text.lower()
    text = re.sub(r'[^a-z0-9]+', '-', text)
    return text.strip('-')

def download_and_convert_logo(url, filename):
    ensure_dir(LOGO_DIR)
    output_path = os.path.join(LOGO_DIR, f"{filename}.webp")
    temp_path = os.path.join(LOGO_DIR, f"{filename}.temp")

    # If it is already downloaded, skip downloading to avoid unnecessary bandwidth
    if os.path.exists(output_path):
        return f"ms-appx:///Assets/apps/{filename}.webp"

    try:
        print(f"Downloading: {url}")
        # Send user-agent to bypass basic bot blocks
        req = urllib.request.Request(
            url, 
            headers={'User-Agent': 'Mozilla/5.0 (Windows NT 10.0; Win64; x64)'}
        )
        with urllib.request.urlopen(req, timeout=10) as response:
            with open(temp_path, 'wb') as f:
                f.write(response.read())

        # Open and convert to WebP
        with Image.open(temp_path) as img:
            # Handle multi-frame ICO files
            if getattr(img, "is_animated", False) or getattr(img, "n_frames", 1) > 1:
                best_frame = 0
                best_size = 0
                for frame in range(getattr(img, "n_frames", 1)):
                    img.seek(frame)
                    size = img.size[0] * img.size[1]
                    if size > best_size:
                        best_size = size
                        best_frame = frame
                img.seek(best_frame)

            # Convert to RGBA
            if img.mode != 'RGBA':
                img = img.convert('RGBA')

            # Contain within 64x64
            img.thumbnail((64, 64), Image.Resampling.LANCZOS)
            
            # Create transparent 64x64 background canvas
            background = Image.new('RGBA', (64, 64), (0, 0, 0, 0))
            # Center the thumbnail
            offset = ((64 - img.size[0]) // 2, (64 - img.size[1]) // 2)
            background.paste(img, offset)
            
            background.save(output_path, 'WEBP', quality=80)

        print(f"Success WebP conversion: {filename}.webp")
        if os.path.exists(temp_path):
            os.remove(temp_path)
        return f"ms-appx:///Assets/apps/{filename}.webp"
    except Exception as e:
        print(f"Failed {filename}: {e}")
        if os.path.exists(temp_path):
            try:
                os.remove(temp_path)
            except:
                pass
        return None

YAML_PATH = os.path.abspath(os.path.join(PROJECT_DIR, '..', 'app-logos.yml'))

def load_logos_yaml():
    logos = {}
    if os.path.exists(YAML_PATH):
        with open(YAML_PATH, 'r', encoding='utf-8') as f:
            for line in f:
                line = line.strip()
                if not line or line.startswith('#'):
                    continue
                if ': ' in line:
                    k, v = line.split(': ', 1)
                    logos[k.strip()] = v.strip()
    return logos

def save_logos_yaml(logos):
    # Set/Overwrite SecureFolderFS logo
    logos['https://github.com/securefolderfs-community/SecureFolderFS'] = 'https://raw.githubusercontent.com/securefolderfs-community/SecureFolderFS/refs/heads/master/src/Platforms/SecureFolderFS.Uno/Assets/AppIcon/PackageLogo.scale-150.png'
    with open(YAML_PATH, 'w', encoding='utf-8') as f:
        f.write("# App URL to Logo Image URL Mappings\n")
        f.write("# Automatically generated and updated by generate-apps.py\n")
        f.write("# You can add or modify mappings here manually.\n\n")
        for k in sorted(logos.keys()):
            f.write(f"{k}: {logos[k]}\n")

def parse_apps_markdown(readme_path, yaml_logos):
    if not os.path.exists(readme_path):
        print(f"README not found at: {readme_path}")
        return []

    with open(readme_path, 'r', encoding='utf-8') as f:
        lines = f.readlines()

    start_idx = -1
    for i, line in enumerate(lines):
        if line.strip().startswith('## 📑 Apps List') or line.strip().startswith('## 📱 Apps List'):
            start_idx = i
            break

    if start_idx == -1:
        print("Could not find '## 📱 Apps List' in README")
        return []

    groups = []
    current_group = None
    current_sub = None

    for line in lines[start_idx + 1:]:
        trimmed = line.strip()

        # Stop if we hit next H2
        if trimmed.startswith('## ') and not (trimmed.startswith('## 📑 Apps List') or trimmed.startswith('## 📱 Apps List')):
            break

        # Main Category
        if trimmed.startswith('### '):
            if current_group:
                groups.append(current_group)
            current_group = {
                "heading": trimmed.replace('### ', '').strip(),
                "subgroups": []
            }
            current_sub = None
            continue

        # Subcategory
        if trimmed.startswith('#### '):
            if not current_group:
                current_group = {"heading": "", "subgroups": []}
            
            clean_heading = re.sub(r'<img[^>]+>', '', trimmed.replace('#### ', '')).strip()
            current_sub = {
                "subheading": clean_heading,
                "apps": []
            }
            current_group["subgroups"].append(current_sub)
            continue

        # App entry
        if trimmed.startswith('- ') or trimmed.startswith('* '):
            name_match = re.search(r'\[(.*?)\]\((.*?)\)', trimmed)
            if not name_match:
                continue

            name, link = name_match.groups()
            
            tag_match = re.search(r'`([^`]+)`', trimmed)
            tag = tag_match.group(1) if tag_match else "WD"

            price = "Free"
            if "💰" in trimmed:
                price = "Paid"
            elif "FOSS" in trimmed:
                price = "FOSS"
            elif "Planned" in trimmed:
                price = "Planned"

            # Check for inline logo comment
            logo_match = re.search(r'<!--\s*logo:\s*(.*?)\s*-->', trimmed, re.IGNORECASE)
            logo_url = logo_match.group(1).strip() if logo_match else None
            if logo_url == 'nan' or (logo_url and not logo_url.startswith('http')):
                logo_url = None

            # Fallback to YAML logos
            if not logo_url:
                logo_url = yaml_logos.get(link)

            if not current_sub:
                current_sub = {"subheading": "", "apps": []}
                if not current_group:
                    current_group = {"heading": "", "subgroups": []}
                current_group["subgroups"].append(current_sub)

            current_sub["apps"].append({
                "name": name,
                "link": link,
                "tag": tag,
                "price": price,
                "logo_url": logo_url
            })

    if current_group:
        groups.append(current_group)

    # Filter out empty entries
    clean_groups = []
    for g in groups:
        non_empty_subs = []
        for sub in g["subgroups"]:
            if len(sub["apps"]) > 0:
                non_empty_subs.append(sub)
        if len(non_empty_subs) > 0:
            g["subgroups"] = non_empty_subs
            clean_groups.append(g)

    return clean_groups

def main():
    # 1. Load existing YAML logos
    yaml_logos = load_logos_yaml()
    yaml_updated = False

    # 2. Scan README and extract/clean comments
    print("Checking README.md for inline logo comments...")
    if os.path.exists(README_PATH):
        with open(README_PATH, 'r', encoding='utf-8') as f:
            readme_lines = f.readlines()

        cleaned_lines = []
        readme_updated = False

        for line in readme_lines:
            # Check for logo comment
            logo_match = re.search(r'<!--\s*logo:\s*(.*?)\s*-->', line, re.IGNORECASE)
            name_match = re.search(r'\[(.*?)\]\((.*?)\)', line)

            if logo_match and name_match:
                logo_content = logo_match.group(1).strip()
                app_url = name_match.group(2)
                
                # Only add valid HTTP URLs to YAML
                if logo_content.startswith('http'):
                    if yaml_logos.get(app_url) != logo_content:
                        yaml_logos[app_url] = logo_content
                        yaml_updated = True

                # Remove logo comment from the line
                line = line.replace(logo_match.group(0), '').rstrip() + '\n'
                readme_updated = True

            cleaned_lines.append(line)

        if readme_updated:
            print("Cleaning logo comments from README.md...")
            with open(README_PATH, 'w', encoding='utf-8') as f:
                f.writelines(cleaned_lines)

    if yaml_updated or not os.path.exists(YAML_PATH):
        print("Updating app-logos.yml...")
        save_logos_yaml(yaml_logos)

    print("Parsing local README.md...")
    parsed = parse_apps_markdown(README_PATH, yaml_logos)
    if not parsed:
        print("Parsing failed.")
        return

    # Load existing JSON if exists to preserve logo URLs
    existing_logos = {}
    if os.path.exists(OUTPUT_JSON_PATH):
        try:
            with open(OUTPUT_JSON_PATH, 'r', encoding='utf-8') as f:
                old_data = json.load(f)
                for group in old_data:
                    for sub in group.get("subgroups", []):
                        for app in sub.get("apps", []):
                            if app.get("logo_url") and app["logo_url"].startswith("ms-appx:///"):
                                existing_logos[app["name"].lower()] = app["logo_url"]
        except Exception as e:
            print(f"Failed to load old JSON: {e}")

    print("Processing app logos...")
    for group in parsed:
        for sub in group["subgroups"]:
            for app in sub["apps"]:
                orig_logo = app["logo_url"]
                app_name_lower = app["name"].lower()

                # If we have an existing local webp path, preserve it
                if app_name_lower in existing_logos:
                    app["logo_url"] = existing_logos[app_name_lower]
                elif orig_logo:
                    filename = slugify(app["name"])
                    local_webp = download_and_convert_logo(orig_logo, filename)
                    if local_webp:
                        app["logo_url"] = local_webp
                    else:
                        app["logo_url"] = None

    # Write output JSON
    ensure_dir(os.path.dirname(OUTPUT_JSON_PATH))
    with open(OUTPUT_JSON_PATH, 'w', encoding='utf-8') as f:
        json.dump(parsed, f, indent=2, ensure_ascii=False)

    print(f"Saved compiled database to: {OUTPUT_JSON_PATH}")

if __name__ == "__main__":
    main()
