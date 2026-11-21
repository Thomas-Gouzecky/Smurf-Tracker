
#!/bin/sh

set -eu

CONNECT_URL="http://debezium:8083"

echo "Waiting for Kafka Connect..."

until curl -fsS "$CONNECT_URL/" > /dev/null; do
    sleep 5
done

echo "Kafka Connect is ready."

echo "Registering auth PostgreSQL connector..."

curl -fsS -X PUT \
    "$CONNECT_URL/connectors/auth-postgres-connector/config" \
    -H "Content-Type: application/json" \
    --data-binary @/connectors/auth-postgres-connector.json

echo ""
echo "Connector configuration applied successfully."

echo "Checking connector status..."

curl -fsS \
    "$CONNECT_URL/connectors/auth-postgres-connector/status"

echo ""
echo "Connector setup complete."
