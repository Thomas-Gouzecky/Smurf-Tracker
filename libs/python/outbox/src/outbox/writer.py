from sqlmodel import Session
from .models import OutboxEventCreate, OutboxEvent


class OutboxWriter:
    def write_event(self, session: Session, event: OutboxEventCreate) -> OutboxEvent:
        """
        Add an event to the outbox within the caller's transaction.

            The caller is responsible for committing or rolling back.

            Args:
                session: The database session.
                event: The event to write.

            Returns:
                The OutboxEvent database model.
        """

        outbox_event = OutboxEvent(
            event_type=event.event_type,
            aggregate_id=event.aggregate_id,
            aggregate_type=event.aggregate_type,
            payload=event.payload,
        )
        session.add(outbox_event)

        return outbox_event
