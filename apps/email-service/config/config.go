package config

import "os"

type Config struct {
	ResendAPIKey string
	ServerPort   string
}

func LoadEnv() *Config {
	return &Config{
		ResendAPIKey: os.Getenv("RESEND_API_KEY"),
		ServerPort:   os.Getenv("SERVER_PORT"),
	}
}