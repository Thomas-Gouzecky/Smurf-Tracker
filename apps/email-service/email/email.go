package email

import (
	"context"

	"github.com/Thomas-Gouzecky/Smurf-Tracker/models"
)

type EmailService struct {
	provider EmailProvider
}

func NewEmailService(provider EmailProvider) *EmailService {
	return &EmailService{
		provider: provider,
	}
}

func (s *EmailService) SendEmail(ctx context.Context, email models.Email) error {
	return s.provider.SendEmail(ctx, email)
}