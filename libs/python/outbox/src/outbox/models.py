from sqlmodel import Field, SQLModel, Column
from sqlalchemy.dialects.postgresql import JSONB, UUID as PostgresUUID
from sqlalchemy import DateTime, Text, text
from typing import Any
from datetime import datetime
from uuid import UUID as UUIDType, uuid4


class OutboxEvent(SQLModel, table=True):
    """
    Represents an event in the outbox pattern.
    """

    __tablename__: str = "outbox_events"

    event_id: UUIDType = Field(
        default_factory=uuid4,
        sa_column=Column(
            PostgresUUID(as_uuid=True),
            primary_key=True,
        ),
        description="Unique identifier for the event.",
    )
    event_type: str = Field(
        ..., sa_column=Column(Text, nullable=False), description="Type of the event."
    )
    aggregate_id: str = Field(
        ...,
        sa_column=Column(Text, nullable=False),
        description="Identifier for the aggregate associated with the event.",
    )
    aggregate_type: str = Field(
        ...,
        sa_column=Column(Text, nullable=False),
        description="Type of the aggregate associated with the event.",
    )
    payload: dict[str, Any] = Field(
        ...,
        sa_column=Column(JSONB, nullable=False),
        description="The data associated with the event.",
    )
    created_at: datetime | None = Field(
        default=None,
        sa_column=Column(
            DateTime(timezone=True),
            server_default=text("CURRENT_TIMESTAMP"),
            nullable=False,
        ),
        description="Timestamp when the event was created.",
    )
