"""Inspect Artemis SQLite profile records without modifying the database."""
import sqlite3
import json
import sys
from datetime import datetime
from pathlib import Path

if "--remove-generated" in sys.argv:
    path = Path("C:/ProgramData/Artemis/artemis.db")
    db = sqlite3.connect(path)
    backup = Path(__file__).resolve().parents[1] / ("artemis-backup-" + datetime.now().strftime("%Y%m%d-%H%M%S") + ".db")
    with sqlite3.connect(backup) as target:
        db.backup(target)
    matches = []
    for id_, text in db.execute('SELECT Id, ProfileConfiguration FROM ProfileContainers'):
        name = json.loads(text).get("Name", "")
        if name.startswith(("Random Color - 15s Hold and Fade", "Random Color - 15s Lite")):
            matches.append((id_, name))
    with db:
        for id_, name in matches:
            db.execute('DELETE FROM ProfileContainers WHERE Id = ?', (id_,))
            print("Removed generated profile:", name)
    print("Backup:", backup)
    print("Remaining profiles:", [json.loads(r[0]).get("Name") for r in db.execute('SELECT ProfileConfiguration FROM ProfileContainers')])
    db.close()
    sys.exit(0)

db = sqlite3.connect("file:C:/ProgramData/Artemis/artemis.db?mode=ro", uri=True)
for name, sql in db.execute("SELECT name, sql FROM sqlite_master WHERE type='table'"):
    if "profile" in name.lower():
        print(name, sql)
        columns = [r[1] for r in db.execute(f'PRAGMA table_info("{name}")')]
        print("Columns:", columns)
        for row in db.execute(f'SELECT * FROM "{name}"'):
            if any("Random Color" in str(value) for value in row):
                print("MATCH:", [(key, str(value)[:1200]) for key, value in zip(columns, row)])
db.close()
