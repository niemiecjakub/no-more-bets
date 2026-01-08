from fbref import FBref


def main():
    scraper = FBref()
    
    try:
        data = scraper.get_premier_league_stats()
        print(data)
    except Exception as e:
        print(f"Error: {e}")


if __name__ == "__main__":
    main()
