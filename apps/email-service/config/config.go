package config

import "os"

type Config struct {
	ResendAPIKey string
	Port         string
}

func LoadEnv() *Config {
	return &Config{
		ResendAPIKey: os.Getenv("RESEND_API_KEY"),
		Port:         os.Getenv("PORT"),
	}
}