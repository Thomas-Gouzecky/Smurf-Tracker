package main

import (
	"log"

	"github.com/Thomas-Gouzecky/Smurf-Tracker/api"
	"github.com/Thomas-Gouzecky/Smurf-Tracker/config"
	"github.com/joho/godotenv"
)

func main() {
	if err := godotenv.Load(); err != nil {
        log.Println("No .env file found; using system environment")
    }
	
	cfg := config.LoadEnv()

	srv := api.NewGinServer(cfg.ServerPort)
	if err := srv.Start(); err != nil {
		log.Fatalf("failed to start server: %v", err)
	}
}