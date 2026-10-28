import pytest
from sqlmodel import SQLModel, Session, create_engine, select
from sqlalchemy.pool import StaticPool

from packages.python.outbox.src.outbox.models import OutboxEvent, OutboxEventCreate
from packages.python.outbox.src.outbox.writer import OutboxWriter


@pytest.fixture
def session():
    engine = create_engine(
        "sqlite://",
        connect_args={"check_same_thread": False},
        poolclass=StaticPool,
    )
    SQLModel.metadata.create_all(engine)
    with Session(engine) as session:
        yield session

    engine.dispose()


def test_write_event_adds_outbox_event_to_session(session):
    event = OutboxEventCreate(
        event_type="GroupChatCreated",
        aggregate_id="42",
        aggregate_type="GroupChat",
        payload={"name": "Gaming Friends"},
    )

    written_event = OutboxWriter().write_event(session, event)

    assert written_event in session.new
    assert isinstance(written_event, OutboxEvent)


def test_write_event_preserves_payload_and_metadata(session):
    event = OutboxEventCreate(
        event_type="GroupChatCreated",
        aggregate_id="42",
        aggregate_type="GroupChat",
        payload={"name": "Gaming Friends", "created_by": "user-123"},
    )

    written_event = OutboxWriter().write_event(session, event)

    assert written_event.event_type == event.event_type
    assert written_event.aggregate_id == event.aggregate_id
    assert written_event.aggregate_type == event.aggregate_type
    assert written_event.payload == event.payload


def test_write_event_is_not_persisted_when_transaction_is_rolled_back(session):
    event = OutboxEventCreate(
        event_type="GroupChatCreated",
        aggregate_id="42",
        aggregate_type="GroupChat",
        payload={"name": "Gaming Friends"},
    )

    OutboxWriter().write_event(session, event)
    session.flush()
    session.rollback()

    assert session.exec(select(OutboxEvent)).all() == []
    session.close()
