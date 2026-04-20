import os
import time
import json
import logging
import requests
from pathlib import Path
from tqdm import tqdm
from concurrent.futures import ThreadPoolExecutor, as_completed

BASE_URL              = "https://localhost:7005"
RESUME_FOLDER         = r"C:\Users\gurup\Downloads\resume-rag-testing\generated_resumes_v2"
BATCH_SIZE            = 10
DELAY_BETWEEN_BATCHES = 2
OUTPUT_FILE           = r"C:\Users\gurup\Downloads\resume-rag-testing\upload_results.json"
SUPPORTED_EXTS        = {".pdf", ".docx", ".doc"}

logging.basicConfig(
    level=logging.INFO,
    format="%(asctime)s  %(levelname)-8s  %(message)s",
    datefmt="%H:%M:%S",
)
log = logging.getLogger(__name__)

UPLOAD_ENDPOINT = f"{BASE_URL}/resume-rag-aiservice/v1/upload-resume"


def _mime(path: Path) -> str:
    return {
        ".pdf":  "application/pdf",
        ".docx": "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ".doc":  "application/msword",
    }.get(path.suffix.lower(), "application/octet-stream")


def upload_single(file_path: Path) -> dict:
    result = {
        "file":         file_path.name,
        "status":       None,
        "candidate_id": None,
        "error":        None,
    }
    try:
        with open(file_path, "rb") as f:
            resp = requests.post(
                UPLOAD_ENDPOINT,
                files={"file": (file_path.name, f, _mime(file_path))},
                timeout=120,
                verify=False
            )
        if resp.status_code == 200:
            data = resp.json()
            result["status"]       = "success"
            result["candidate_id"] = data.get("candidateId")
            log.info("✓  %-45s  →  candidateId: %s", file_path.name, result["candidate_id"])
        else:
            result["status"] = f"http_{resp.status_code}"
            result["error"]  = resp.text[:300]
            log.warning("✗  %-45s  →  HTTP %s  %s", file_path.name, resp.status_code, resp.text[:200])
    except Exception as exc:
        result["status"] = "exception"
        result["error"]  = str(exc)
        log.error("✗  %-45s  →  %s", file_path.name, exc)
    return result


def chunked(lst, n):
    for i in range(0, len(lst), n):
        yield lst[i : i + n]


def main():
    folder = Path(RESUME_FOLDER)
    if not folder.exists():
        log.error("Resume folder not found: %s", folder.resolve())
        return

    files = [p for p in folder.iterdir() if p.suffix.lower() in SUPPORTED_EXTS]
    if not files:
        log.error("No supported files found in %s", folder.resolve())
        return

    log.info("Found %d resume(s) in '%s'", len(files), folder)
    log.info("Batch size: %d  |  Total batches: %d", BATCH_SIZE, -(-len(files) // BATCH_SIZE))

    all_results   = []
    success_count = 0
    fail_count    = 0

    batches = list(chunked(files, BATCH_SIZE))
    for batch_num, batch in enumerate(batches, 1):
        log.info("── Batch %d / %d ─────────────────────────", batch_num, len(batches))

        with ThreadPoolExecutor(max_workers=BATCH_SIZE) as pool:
            futures = {pool.submit(upload_single, f): f for f in batch}
            for future in tqdm(as_completed(futures), total=len(batch), desc=f"Batch {batch_num}"):
                res = future.result()
                all_results.append(res)
                if res["status"] == "success":
                    success_count += 1
                else:
                    fail_count += 1

        if batch_num < len(batches):
            log.info("Sleeping %ds before next batch…", DELAY_BETWEEN_BATCHES)
            time.sleep(DELAY_BETWEEN_BATCHES)

    with open(OUTPUT_FILE, "w") as f:
        json.dump(all_results, f, indent=2)

    log.info("")
    log.info("═══════════════════════════════════════")
    log.info("Total : %d  |  ✓ Success: %d  |  ✗ Failed: %d", len(all_results), success_count, fail_count)
    log.info("Results saved → %s", OUTPUT_FILE)


if __name__ == "__main__":
    main()
