package email

import (
	"context"

	"github.com/Thomas-Gouzecky/Smurf-Tracker/models"
	"github.com/Thomas-Gouzecky/Smurf-Tracker/renderer"
)

type EmailService struct {
	provider EmailProvider
	renderer *renderer.Renderer
}

func NewEmailService(provider EmailProvider, renderer *renderer.Renderer) *EmailService {
	return &EmailService{
		provider: provider,
		renderer: renderer,
	}
}

func (s *EmailService) SendEmail(ctx context.Context, email models.Email) error {
	templateName := email.Type + ".html"

    html, err := s.renderer.Render(templateName, email.Data)
    if err != nil {
        return err
    }

    email.HTML = html

    return s.provider.SendEmail(ctx, email)
}