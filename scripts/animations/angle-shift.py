import re

angle = 45 * 1

def decrease_values(text):
    cylinder_pattern = ( r'("Cylinder"\s*:\s*\{[^}]*?"rotationZ"\s*:\s*)(-?\d+(?:\.\d+)?)' )

    def rotation_replacer(match):
        prefix = match.group(1)
        num = float(match.group(2))
        return f'{prefix}{num - angle:.1f}'

    text = re.sub(cylinder_pattern, rotation_replacer, text)

    return text

def decrease_values2(text):
    cylinder_pattern = ( r'("Cylinder"\s*:\s*\[\s*(.*?)\s*\])' )

    def cylinder_replacer(match):
        full = match.group(1)
        inside = match.group(2)

        # Split inside elements
        parts = [p.strip() for p in inside.split(",")]

        if len(parts) >= 6:
            # last element is rotationZ
            last = float(parts[-1])
            parts[-1] = f"{last - angle:.1f}"

        new_array = ", ".join(parts)
        return f'"Cylinder": [{new_array}]'

    text = re.sub(cylinder_pattern, cylinder_replacer, text)

    return text


# Example usage
if __name__ == "__main__":
    with open("inout.json", "r") as f:
        data = f.read()

    updated = decrease_values2(data)

    with open("inout.json", "w") as f:
        f.write(updated)

    print("Done! Updated values written to output.json")
