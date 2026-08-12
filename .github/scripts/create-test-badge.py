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


def totals(path: Path) -> tuple[int, int, int]:
    matches = list(SUMMARY.finditer(path.read_text(encoding="utf-8")))
    if not matches:
        raise ValueError(f"No test summary found in {path}")

    return tuple(
        sum(int(match.group(field)) for match in matches)
        for field in ("failed", "passed", "total")
    )


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
