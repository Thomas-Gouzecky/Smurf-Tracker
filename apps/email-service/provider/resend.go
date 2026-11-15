package provider

import (
	"context"
	"log"

	"github.com/Thomas-Gouzecky/Smurf-Tracker/models"
	"github.com/resend/resend-go/v4"
)

type ResendProvider struct {
	client *resend.Client
}

func (p *ResendProvider) SendEmail(ctx context.Context, email models.Email) error {
    params := &resend.SendEmailRequest{
        From:    "noreply@tomcheese.ca",
        To:      []string{email.To},
        Subject: email.Subject,
        Html:    email.HTML,
    }

    sent, err := p.client.Emails.Send(params)
    if err != nil {
        return err
    }

    log.Printf("email sent: %s", sent.Id)
	return nil
}

func NewResendProvider(apiKey string) *ResendProvider {
	client := resend.NewClient(apiKey)
	return &ResendProvider{
		client: client,
	}
}