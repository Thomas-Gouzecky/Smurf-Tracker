package models

type Email struct {
	Template string    `json:"template" binding:"required"`
	To       string    `json:"to" binding:"required,email"`
	Subject  string    `json:"subject" binding:"required"`
	Data     EmailData `json:"data"`
	Body     string    `json:"body"`
	HTML     string    `json:"html"`
}

type EmailData struct {
	Username         string `json:"username"`
	VerificationLink string `json:"verification_link"`
	ResetLink        string `json:"reset_link"`
	ExpirationTime   string `json:"expiration_time"`
}