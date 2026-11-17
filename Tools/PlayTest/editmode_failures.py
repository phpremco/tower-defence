#!/usr/bin/env python3
"""Totals and failed tests of an NUnit results file (default: worker w6's EditMode run).
  python Tools/PlayTest/editmode_failures.py [results.xml]"""
import re, sys
path = sys.argv[1] if len(sys.argv) > 1 else r"C:\Users\remco\TowerDefenseWorkers\w6\Logs\results.xml"
text = open(path, encoding="utf-8").read()
run = re.search(r"<test-run [^>]*", text).group(0)
print(" ".join(re.findall(r'(?:total|passed|failed|skipped)="\d+"', run)))
for case in re.finditer(r'<test-case [^>]*fullname="([^"]*)"[^>]*result="Failed"[^>]*>(.*?)</test-case>', text, re.S):
    message = re.search(r"<message><!\[CDATA\[(.*?)\]\]>", case.group(2), re.S)
    print(case.group(1))
    print("    " + (message.group(1) if message else "")[:900].replace("\n", "\n    "))
