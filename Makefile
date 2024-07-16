DB_PASSWORD ?= changethisdefaultpassword

.PHONY: help build deploy sync

# help target should appear first so it's the default
help: ## this list
	@grep -E '^[a-zA-Z_-]+:.*?## .*$$' $(MAKEFILE_LIST) | sort | awk 'BEGIN {FS = ":.*?## "}; {printf "\033[36m%-30s\033[0m %s\n", $$1, $$2}'

build: ## build the serverless project
	@sam build
	
deploy: ## deploy the serverless project to aws
	@sam deploy --guided --parameter-overrides DBPassword=$(DB_PASSWORD)

sync: ## sync local changes to the cloud cloudformation deployment (can cause drift)
	@sam sync