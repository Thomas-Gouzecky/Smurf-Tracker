package email

import (
	"context"
	"errors"
	"strings"
	"testing"

	"github.com/Thomas-Gouzecky/Smurf-Tracker/email"
	"github.com/Thomas-Gouzecky/Smurf-Tracker/models"
	"github.com/Thomas-Gouzecky/Smurf-Tracker/renderer"
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

func newEmailService(t *testing.T, provider *fakeProvider) *email.EmailService {
	t.Helper()

	emailRenderer, err := renderer.NewRenderer()
	if err != nil {
		t.Fatalf("failed to create renderer: %v", err)
	}

	return email.NewEmailService(provider, emailRenderer)
}

func TestEmailServiceSendEmailDelegatesToProvider(t *testing.T) {
	ctx := context.WithValue(context.Background(), "request-id", "test-request")
	expectedEmail := models.Email{
		To:      "recipient@example.com",
		Subject: "Test email",
		Template:    "account_verification",
		Data: models.EmailData{
			Username:         "Test User",
			VerificationLink: "https://example.com/verify",
			ExpirationTime:   "24 hours",
		},
	}
	provider := &fakeProvider{}
	service := newEmailService(t, provider)

	err := service.SendEmail(ctx, expectedEmail)

	if err != nil {
		t.Fatalf("SendEmail returned an unexpected error: %v", err)
	}
	if provider.calledCtx != ctx {
		t.Fatal("SendEmail did not forward the context")
	}
	if provider.calledEmail.To != expectedEmail.To ||
		provider.calledEmail.Subject != expectedEmail.Subject ||
		provider.calledEmail.Template != expectedEmail.Template ||
		provider.calledEmail.Data != expectedEmail.Data {
		t.Errorf("SendEmail forwarded email %+v, want %+v", provider.calledEmail, expectedEmail)
	}
	if !strings.Contains(provider.calledEmail.HTML, "Test User") ||
		!strings.Contains(provider.calledEmail.HTML, "https://example.com/verify") ||
		!strings.Contains(provider.calledEmail.HTML, "24 hours") {
		t.Errorf("SendEmail did not render template data into HTML: %s", provider.calledEmail.HTML)
	}
}

func TestEmailServiceSendEmailReturnsProviderError(t *testing.T) {
	expectedErr := errors.New("provider unavailable")
	provider := &fakeProvider{err: expectedErr}
	service := newEmailService(t, provider)

	err := service.SendEmail(context.Background(), models.Email{
		Template: "account_verification",
		Data: models.EmailData{
			Username:         "Test User",
			VerificationLink: "https://example.com/verify",
			ExpirationTime:   "24 hours",
		},
	})

	if !errors.Is(err, expectedErr) {
		t.Errorf("SendEmail returned error %v, want %v", err, expectedErr)
	}
}
