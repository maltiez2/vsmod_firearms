import re

def decrease_frame_numbers(text):
    # Regex matches: "frame": <integer>
    pattern = r'("frame"\s*:\s*)(-?\d+)'

    def replacer(match):
        prefix = match.group(1)
        num = int(match.group(2))
        return f'{prefix}{num - 15}'

    return re.sub(pattern, replacer, text)


# Example usage
if __name__ == "__main__":
    with open("input.json", "r") as f:
        data = f.read()

    updated_data = decrease_frame_numbers(data)

    with open("output.json", "w") as f:
        f.write(updated_data)

    print("Done! Updated numbers written to output.json")
