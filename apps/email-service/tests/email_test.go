package email

import (
	"context"
	"errors"
	"testing"

	"github.com/Thomas-Gouzecky/Smurf-Tracker/email"
	"github.com/Thomas-Gouzecky/Smurf-Tracker/models"
)

type fakeProvider struct {
	calledEmail models.Email
	calledCtx   context.Context
	err         error
}

func (p *fakeProvider) SendEmail(ctx context.Context, email models.Email) error {
	p.calledCtx = ctx
	p.calledEmail = email
	return p.err
}

func TestEmailServiceSendEmailDelegatesToProvider(t *testing.T) {
	ctx := context.WithValue(context.Background(), "request-id", "test-request")
	expectedEmail := models.Email{
		To:      "recipient@example.com",
		Subject: "Test email",
		HTML:    "<p>Hello</p>",
	}
	provider := &fakeProvider{}
	service := email.NewEmailService(provider)

	err := service.SendEmail(ctx, expectedEmail)

	if err != nil {
		t.Fatalf("SendEmail returned an unexpected error: %v", err)
	}
	if provider.calledCtx != ctx {
		t.Fatal("SendEmail did not forward the context")
	}
	if provider.calledEmail != expectedEmail {
		t.Errorf("SendEmail forwarded email %+v, want %+v", provider.calledEmail, expectedEmail)
	}
}

func TestEmailServiceSendEmailReturnsProviderError(t *testing.T) {
	expectedErr := errors.New("provider unavailable")
	provider := &fakeProvider{err: expectedErr}
	service := email.NewEmailService(provider)

	err := service.SendEmail(context.Background(), models.Email{})

	if !errors.Is(err, expectedErr) {
		t.Errorf("SendEmail returned error %v, want %v", err, expectedErr)
	}
}
