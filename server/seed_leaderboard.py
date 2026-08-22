"""
리더보드 더미데이터 주입 스크립트.
실행: python seed_leaderboard.py
필요: pip install requests

BASE_URL을 바꾸면 로컬(http://localhost:8000)에도 그대로 쓸 수 있음.
"""

import requests

BASE_URL = "https://pooshout.onrender.com"

# purity >= 90 -> "일급수 황금 물방울", 그 외 -> "아리수 물방울" (ScoreManager.cs 기준과 동일)
DUMMY_ENTRIES = [
    {"playerName": "KHW", "purity": 95, "grade": "일급수 황금 물방울"},
    {"playerName": "YRI", "purity": 92, "grade": "일급수 황금 물방울"},
    {"playerName": "JHS", "purity": 88, "grade": "아리수 물방울"},
    {"playerName": "MJK", "purity": 85, "grade": "아리수 물방울"},
    {"playerName": "SYH", "purity": 79, "grade": "아리수 물방울"},
    {"playerName": "DHL", "purity": 73, "grade": "아리수 물방울"},
    {"playerName": "BJP", "purity": 68, "grade": "아리수 물방울"},
    {"playerName": "EJC", "purity": 61, "grade": "아리수 물방울"},
    {"playerName": "WSK", "purity": 55, "grade": "아리수 물방울"},
    {"playerName": "HYJ", "purity": 48, "grade": "아리수 물방울"},
]


def main():
    for entry in DUMMY_ENTRIES:
        resp = requests.post(f"{BASE_URL}/leaderboard", json=entry, timeout=10)
        resp.raise_for_status()
        result = resp.json()
        print(f"{entry['playerName']:>4} (purity={entry['purity']:>3}) -> "
              f"rank={result['rank']}, displayName={result['displayName']}")

    print("\n현재 상위 10명:")
    top10 = requests.get(f"{BASE_URL}/leaderboard", timeout=10).json()["entries"]
    for i, e in enumerate(top10, 1):
        print(f"{i:>2}. {e['displayName']} - {e['purity']} ({e['grade']})")


if __name__ == "__main__":
    main()
