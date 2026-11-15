package main

import (
	"context"
	"log"

	"github.com/Thomas-Gouzecky/Smurf-Tracker/config"
	"github.com/Thomas-Gouzecky/Smurf-Tracker/email"
	"github.com/Thomas-Gouzecky/Smurf-Tracker/models"
	"github.com/Thomas-Gouzecky/Smurf-Tracker/provider"
	"github.com/joho/godotenv"
)

func main() {
	if err := godotenv.Load(); err != nil {
        log.Println("No .env file found; using system environment")
    }
	
	cfg := config.LoadEnv()

	emailService := email.NewEmailService(provider.NewResendProvider(cfg.ResendAPIKey))

	email := models.Email{
		To:      "thomasjgouzecky@gmail.com",
		Subject: "Hello World",
		HTML:    "<p>Congrats on sending your <strong>first email</strong>!</p>",
	}

	err := emailService.SendEmail(context.Background(), email)
	if err != nil {
		log.Fatal(err)
	}
}