import asyncio
from dotenv import load_dotenv
from agents.group_chat import run_betting_analysis


def main():
    load_dotenv()
    result = asyncio.run(run_betting_analysis("Analyze Leeds vs Fulham", verbose=True))
    print(result)

if __name__ == "__main__":
    main()
