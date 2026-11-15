package renderer

import (
	"bytes"
	"embed"
	"html/template"
)

type Renderer struct {
	templates *template.Template
}

//go:embed templates/*.html
var templateFiles embed.FS

func NewRenderer() (*Renderer, error) {
	templates, err := template.ParseFS(templateFiles, "templates/*.html")
	if err != nil {
		return nil, err
	}

	return &Renderer{
		templates: templates,
	}, nil
}

func (r *Renderer) Render(name string, data any) (string, error) {
	var buf bytes.Buffer

	err := r.templates.ExecuteTemplate(&buf, name, data)
	if err != nil {
		return "", err
	}

	return buf.String(), nil
}