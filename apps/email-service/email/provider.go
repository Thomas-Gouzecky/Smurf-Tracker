package email

import (
	"context"

	"github.com/Thomas-Gouzecky/Smurf-Tracker/models"
)

type EmailProvider interface {
	SendEmail(ctx context.Context, email models.Email) error
}