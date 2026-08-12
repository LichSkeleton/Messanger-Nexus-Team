#!/usr/bin/env python3

import argparse
import json
import re
from pathlib import Path


SUMMARY = re.compile(
    r"Failed:\s*(?P<failed>\d+),\s*"
    r"Passed:\s*(?P<passed>\d+),.*?"
    r"Total:\s*(?P<total>\d+)",
)

VSTEST_BLOCK = re.compile(
    r"Test Run (?:Successful|Failed)\.(?P<body>.*?)(?=Total time:)",
    re.DOTALL,
)


def value(body: str, label: str) -> int:
    match = re.search(rf"^\s*{label}:\s*(\d+)\s*$", body, re.MULTILINE)
    return int(match.group(1)) if match else 0


def totals(path: Path) -> tuple[int, int, int]:
    text = path.read_text(encoding="utf-8")
    matches = list(SUMMARY.finditer(text))
    if matches:
        return tuple(
            sum(int(match.group(field)) for match in matches)
            for field in ("failed", "passed", "total")
        )

    blocks = [match.group("body") for match in VSTEST_BLOCK.finditer(text)]
    if blocks:
        return (
            sum(value(block, "Failed") for block in blocks),
            sum(value(block, "Passed") for block in blocks),
            sum(value(block, "Total tests") for block in blocks),
        )

    raise ValueError(f"No test summary found in {path}")


def main() -> None:
    parser = argparse.ArgumentParser()
    parser.add_argument("--label", required=True)
    parser.add_argument("--baseline-log", type=Path, required=True)
    parser.add_argument("--regression-log", type=Path, required=True)
    parser.add_argument("--output", type=Path, required=True)
    args = parser.parse_args()

    baseline_failed, baseline_passed, baseline_total = totals(args.baseline_log)
    regression_failed, regression_passed, regression_total = totals(args.regression_log)
    passed = baseline_passed + regression_passed
    total = baseline_total + regression_total

    if baseline_failed:
        color = "critical"
    elif passed == total:
        color = "brightgreen"
    else:
        color = "orange"

    args.output.parent.mkdir(parents=True, exist_ok=True)
    args.output.write_text(
        json.dumps(
            {
                "schemaVersion": 1,
                "label": args.label,
                "message": f"{passed}/{total} passing",
                "color": color,
            },
            separators=(",", ":"),
        )
        + "\n",
        encoding="utf-8",
    )


if __name__ == "__main__":
    main()
