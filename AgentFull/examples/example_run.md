# Example Run

From `Assets/AgentFull`:

```bash
pip install -r requirements.txt
python run.py --task auto_discover_and_implement --mock
```

Useful variants:

```bash
python run.py --task scan_project --mock
python run.py --task analyze_scripts --mock
python run.py --task generate_feature --mock
python run.py --task auto_discover_and_implement --model deepseek
```

Reports are written to:

```text
AgentFull/reports/<date>/<candidate_id>_<safe_feature_name>/
```
