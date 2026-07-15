import re
import os

def main():
    readme_path = 'README.md'
    output_path = 'apps_logo.yml'

    # 1. Read existing logos from apps_logo.yml (if any)
    logos = {} # link -> {name, logo}
    if os.path.exists(output_path):
        with open(output_path, 'r', encoding='utf-8') as f:
            yml_content = f.read()
        
        # Simple regex parser for our specific format:
        # - name: "..."
        #   link: "..."
        #   logo: "..."
        entry_pattern = re.compile(
            r'-\s*name:\s*"(.*?)"\s*\n\s*link:\s*"(.*?)"\s*\n\s*logo:\s*"(.*?)"',
            re.MULTILINE
        )
        for name, link, logo in entry_pattern.findall(yml_content):
            # Unescape quotes
            name_clean = name.replace('\\"', '"')
            logos[link.strip()] = {
                'name': name_clean,
                'logo': logo.strip()
            }

    if not os.path.exists(readme_path):
        print(f"Error: {readme_path} not found.")
        return

    with open(readme_path, 'r', encoding='utf-8') as f:
        content = f.read()

    # 2. Extract new logos from README.md
    # We find all matches first
    # Regex captures: Group 1: Name, Group 2: Link, Group 3: Logo URL
    extract_pattern = re.compile(
        r'\[([^\]]+)\]\((https?://[^\)]+)\).*?<!--\s*logo:\s*(https?://[^\s>]+)\s*-->',
        re.IGNORECASE
    )
    new_matches = extract_pattern.findall(content)

    if not new_matches and not logos:
        print("No logo comments found and no existing logos to write.")
        return

    # Add/Update the extracted logos in our dictionary
    for name, link, logo in new_matches:
        logos[link.strip()] = {
            'name': name.strip(),
            'logo': logo.strip()
        }

    # 3. Remove logo comments from README.md
    # We replace any sequence of [whitespace]<!-- logo: ... --> with empty string
    cleaned_content = re.sub(
        r'\s*<!--\s*logo:\s*(https?://[^\s>]+)\s*-->',
        '',
        content
    )

    # 4. Write back the updated README.md
    with open(readme_path, 'w', encoding='utf-8') as f:
        f.write(cleaned_content)

    # 5. Write the updated apps_logo.yml
    yaml_lines = []
    # Sort by name for a clean ordered list
    sorted_logos = sorted(logos.items(), key=lambda item: item[1]['name'].lower())
    for link, data in sorted_logos:
        name_escaped = data['name'].replace('"', '\\"')
        yaml_lines.append(f'- name: "{name_escaped}"')
        yaml_lines.append(f'  link: "{link}"')
        yaml_lines.append(f'  logo: "{data["logo"]}"')

    with open(output_path, 'w', encoding='utf-8') as f:
        if yaml_lines:
            f.write('\n'.join(yaml_lines) + '\n')
        else:
            f.write('')

    print(f"Processed: Added/updated logos. Total in YML: {len(sorted_logos)}. Cleaned comments from README.")

if __name__ == '__main__':
    main()
