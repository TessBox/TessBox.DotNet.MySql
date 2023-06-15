# Ensure docker is installed
# Remove if exist the image mysql/database/test

# Build image, inside test directory

docker build . -t  mysql/database/test

# Run Docker compose, inside test directory
docker compose up