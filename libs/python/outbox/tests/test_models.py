from uuid import UUID

from src.outbox.models import OutboxEvent


def test_create_outbox_event():
    event = OutboxEvent(
        event_type="GroupChatCreated",
        aggregate_id="42",
        aggregate_type="GroupChat",
        payload={
            "name": "Gaming Friends",
            "created_by": "user-123",
        },
    )

    assert event.event_type == "GroupChatCreated"
    assert event.aggregate_id == "42"
    assert event.aggregate_type == "GroupChat"
    assert event.payload == {
        "name": "Gaming Friends",
        "created_by": "user-123",
    }


def test_event_id_defaults_to_uuid():
    event = OutboxEvent(
        event_type="GroupChatCreated",
        aggregate_id="42",
        aggregate_type="GroupChat",
        payload={"name": "Gaming Friends"},
    )

    assert event.event_id is not None
    assert isinstance(event.event_id, UUID)


def test_created_at_defaults_to_none():
    event = OutboxEvent(
        event_type="GroupChatCreated",
        aggregate_id="42",
        aggregate_type="GroupChat",
        payload={"name": "Gaming Friends"},
    )

    assert event.created_at is None


def test_model_uses_expected_table_name():
    assert OutboxEvent.__tablename__ == "outbox_events"
