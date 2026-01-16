# Test Fixtures

This directory contains real-world test data (HTML and JSON) used by the test suite to ensure tests run fast and deterministically without requiring network access.

## Structure

```
fixtures/
├── betclic/
│   ├── premier_league_page.html
│   ├── match_page.html
│   └── match_page_expanded.html
├── fbref/
│   ├── premier_league_stats.html
│   ├── club_page_arsenal.html
│   ├── club_players_arsenal.html
│   └── club_games_arsenal.html
├── premierinjuries/
│   ├── premier_league_injuries.html
│   └── injury_table.html
├── rotowire/
│   └── lineups_page.html
├── soccerdata/
│   ├── match_previews_upcoming.json
│   ├── match_preview.json
│   ├── head_to_head.json
│   └── matches_league.json
└── web_search/
    ├── search_results_sample.json
    └── news_results_sample.json
```

## Populating Fixtures

To populate or refresh the fixtures with real data, run:

```bash
python tests/fixtures/populate_fixtures.py
```

**Requirements:**
- Network access
- Valid `SOCCERDATA_API_KEY` environment variable (for SoccerData fixtures)
- All service dependencies installed

**Note:** This script makes real API calls and web requests. Use responsibly and respect rate limits.

## Using Fixtures in Tests

Fixtures are loaded in tests using the helper functions from `tests/conftest.py`:

```python
from pathlib import Path

def test_example(fixtures_dir):
    fixture_path = fixtures_dir / "betclic" / "premier_league_page.html"
    html = fixture_path.read_text(encoding='utf-8')
    # Use html in test...
```

Or create pytest fixtures:

```python
@pytest.fixture
def betclic_premier_league_html(fixtures_dir):
    """Load real Betclic Premier League page HTML."""
    fixture_path = fixtures_dir / "betclic" / "premier_league_page.html"
    return fixture_path.read_text(encoding='utf-8')
```

## When to Refresh Fixtures

Refresh fixtures when:
- The structure of scraped HTML changes significantly
- API response formats are updated
- New test cases require different data
- Fixtures become outdated or incomplete

## Version Control

- Small fixture files (< 1MB) should be committed to git
- Large fixture files should be added to `.gitignore`
- Consider compressing large HTML files if needed

## Best Practices

1. **Use real data**: Fixtures should represent actual API/HTML responses
2. **Keep updated**: Refresh fixtures periodically to catch breaking changes
3. **Document changes**: Note any manual edits to fixtures
4. **Test with fixtures**: Always use fixtures in tests, never make real network calls
5. **Validate structure**: Ensure fixtures match expected data models
