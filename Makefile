.PHONY: env

env:
	@if [ -f .env ]; then \
		echo ".env already exists — not overwriting"; \
	else \
		cp .env.example .env; \
		echo "Created .env — fill in API keys"; \
	fi
