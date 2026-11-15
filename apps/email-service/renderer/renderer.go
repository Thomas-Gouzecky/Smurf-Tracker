package renderer

import (
	"bytes"
	"html/template"
)

type Renderer struct {
    templates *template.Template
}

func NewRenderer() (*Renderer, error) {
    templates, err := template.ParseGlob("./templates/*.html")
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