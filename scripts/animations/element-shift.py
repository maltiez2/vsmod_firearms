import re

regex1 = r'"cylinder_cover_0_1"'
regex2 = r'"cylinder_cover_0_3"'

index = 2

list1 = [r'"cylinder_cover_0_1"', r'"cylinder_cover_0_50"', r'"cylinder_cover_0_43"', r'"cylinder_cover_0_36"', r'"cylinder_cover_0_29"', r'"cylinder_cover_0_22"', r'"cylinder_cover_0_15"', r'"cylinder_cover_0_8"'];
list2 = [r'"cylinder_cover_0_3"', r'"cylinder_cover_0_52"', r'"cylinder_cover_0_45"', r'"cylinder_cover_0_38"', r'"cylinder_cover_0_31"', r'"cylinder_cover_0_24"', r'"cylinder_cover_0_17"', r'"cylinder_cover_0_10"'];

subst1 = list1[index - 1]
subst2 = list2[index - 1]

with open("input.json", "r") as f:
        data = f.read()

result = re.sub(regex1, subst1, data, 0, re.MULTILINE)
result = re.sub(regex2, subst2, result, 0, re.MULTILINE)

with open("output.json", "w") as f:
        f.write(result)

