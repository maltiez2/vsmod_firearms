import pythonmonkey as pm
import argparse
import os
from glob import iglob

def format(path):
    formatter = pm.require('./formatter')
    f = open(path, "r", encoding='utf-8-sig')
    content = formatter.stringify(f.read(), {"maxLength": length})
    f.close()
    f = open(path, "w", encoding='utf-8-sig')
    f.write(content)
    f.close()

parser = argparse.ArgumentParser(description="Requires 'pip install pythonmonkey'")
parser.add_argument("-f", "--file", default="input.json", help="path to file")
parser.add_argument("-l", "--length", default=180, help="max width")
parser.add_argument("-a", "--all", action="store_true", help="format all recursively")
parser.add_argument("-d", "--directory", default="itemtypes", help="look for specific folder in the path")
args = parser.parse_args()
file = args.file
length = args.length
directory = args.directory

if (args.all):
    rootdir_glob = f'{directory}/**/*.json'
    file_list = [f for f in iglob(rootdir_glob, recursive=True) if os.path.isfile(f)]
    for f in file_list:
        print(f"formatting: {f}")
        format(f)
else:
    print(f"formatting: {file}")
    format(file)
