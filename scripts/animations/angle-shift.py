import re

def decrease_values(text):
    cylinder_pattern = (
        r'("Cylinder"\s*:\s*\{[^}]*?"rotationZ"\s*:\s*)(-?\d+(?:\.\d+)?)'
    )

    def rotation_replacer(match):
        prefix = match.group(1)
        num = float(match.group(2))
        return f'{prefix}{num - 45 * 1:.1f}'

    text = re.sub(cylinder_pattern, rotation_replacer, text)

    return text


# Example usage
if __name__ == "__main__":
    with open("input.json", "r") as f:
        data = f.read()

    updated = decrease_values(data)

    with open("output.json", "w") as f:
        f.write(updated)

    print("Done! Updated values written to output.json")
