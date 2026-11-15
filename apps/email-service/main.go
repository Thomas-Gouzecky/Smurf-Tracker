package main

import (
	"log"

	"github.com/Thomas-Gouzecky/Smurf-Tracker/api"
	"github.com/Thomas-Gouzecky/Smurf-Tracker/config"
	"github.com/Thomas-Gouzecky/Smurf-Tracker/email"
	"github.com/Thomas-Gouzecky/Smurf-Tracker/provider"
	"github.com/Thomas-Gouzecky/Smurf-Tracker/renderer"
	"github.com/joho/godotenv"
)

func main() {
	if err := godotenv.Load(); err != nil {
        log.Println("No .env file found; using system environment")
    }
	
	cfg := config.LoadEnv()

	renderer, err := renderer.NewRenderer()
	if err != nil {
		log.Fatalf("failed to create renderer: %v", err)
	}

	emailProvider := provider.NewResendProvider(cfg.ResendAPIKey)
	emailService := email.NewEmailService(emailProvider, renderer)

	srv := api.NewGinServer(cfg.ServerPort, emailService)
	if err := srv.Start(); err != nil {
		log.Fatalf("failed to start server: %v", err)
	}
}