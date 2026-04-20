import json
import time
import logging
import requests
from datetime import datetime
from pathlib import Path

BASE_URL        = "https://localhost:7005"
JD_FILE         = r"C:\Users\gurup\Downloads\resume-rag-testing\job_descriptions.txt"
OUTPUT_FILE     = r"C:\Users\gurup\Downloads\resume-rag-testing\job_ids.json"
DELAY_SEC       = 1
JD_SEPARATOR    = "==="
LABEL_PREFIX    = "LABEL:"
CONTENT_START   = "---"

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s  %(levelname)-8s  %(message)s",
    datefmt="%H:%M:%S",
)
log = logging.getLogger(__name__)

POST_JOB_ENDPOINT = f"{BASE_URL}/resume-rag-aiservice/v1/post-job"


def parse_job_descriptions(file_path: str) -> list[dict]:
    text = Path(file_path).read_text(encoding="utf-8")
    raw_blocks = [b.strip() for b in text.split(JD_SEPARATOR) if b.strip()]

    jobs = []
    for block in raw_blocks:
        lines = block.splitlines()
        label = ""
        content_lines = []
        in_content = False

        for line in lines:
            stripped = line.strip()
            if stripped.startswith(LABEL_PREFIX) and not in_content:
                label = stripped[len(LABEL_PREFIX):].strip()
            elif stripped == CONTENT_START and not in_content:
                in_content = True
            elif in_content:
                content_lines.append(line)

        content = "\n".join(content_lines).strip()
        if label and content:
            jobs.append({"label": label, "text": content})

    return jobs


def post_job(label: str, text: str) -> dict:
    result = {
        "label":     label,
        "job_id":    None,
        "status":    None,
        "error":     None,
        "posted_at": datetime.utcnow().isoformat(),
    }
    try:
        resp = requests.post(
            POST_JOB_ENDPOINT,
            data=text.strip(),
            headers={"Content-Type": "text/plain; charset=utf-8"},
            timeout=60,
            verify=False
        )
        if resp.status_code == 200:
            data = resp.json()
            result["status"]  = "success"
            result["job_id"]  = data.get("JobId") or data.get("jobId")
            log.info("✓  %-50s  →  JobId: %s", label, result["job_id"])
        else:
            result["status"] = f"http_{resp.status_code}"
            result["error"]  = resp.text[:300]
            log.warning("✗  %-50s  →  HTTP %s", label, resp.status_code)
    except Exception as exc:
        result["status"] = "exception"
        result["error"]  = str(exc)
        log.error("✗  %-50s  →  %s", label, exc)
    return result


def main():
    jd_path = Path(JD_FILE)
    if not jd_path.exists():
        log.error("Job descriptions file not found: %s", jd_path.resolve())
        return

    jobs = parse_job_descriptions(JD_FILE)
    if not jobs:
        log.error("No job descriptions parsed from %s — check file format.", JD_FILE)
        return

    log.info("Parsed %d job description(s) from '%s'", len(jobs), JD_FILE)

    results = []
    for jd in jobs:
        res = post_job(jd["label"], jd["text"])
        results.append(res)
        time.sleep(DELAY_SEC)

    with open(OUTPUT_FILE, "w") as f:
        json.dump(results, f, indent=2)

    successes = [r for r in results if r["status"] == "success"]

    log.info("")
    log.info("════════════════════════════════════════════")
    log.info("Done.  %d / %d succeeded.", len(successes), len(results))
    log.info("Results written → %s", OUTPUT_FILE)
    log.info("")
    log.info("Job IDs:")
    for r in successes:
        log.info("  %-50s  %s", r["label"], r["job_id"])


if __name__ == "__main__":
    main()
