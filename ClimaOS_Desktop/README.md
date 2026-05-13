# ClimaOS Desktop

Fluxul de resetare a parolei folosește acum un cod trimis pe email:

1. utilizatorul introduce emailul
2. primește un cod numeric
3. introduce codul în aplicație
4. setează parola nouă

Codurile sunt invalidate automat când expiră, la emiterea unui cod nou și după folosirea cu succes.

## Configurare email

Varianta cea mai simplă este să completezi fișierul local [smtp.settings.local.json](/Users/h3mbyy/Documents/Practica-Adina-ClimaOS/ClimaOS_Desktop/smtp.settings.local.json). Fișierul este ignorat de Git și se copiază la rulare.

Exemplul de structură este în [smtp.settings.example.json](/Users/h3mbyy/Documents/Practica-Adina-ClimaOS/ClimaOS_Desktop/smtp.settings.example.json).

Dacă preferi, poți folosi și variabile de mediu. Acestea au prioritate peste fișierul local:

- `CLIMAOS_SMTP_HOST`
- `CLIMAOS_SMTP_PORT`
- `CLIMAOS_SMTP_USERNAME`
- `CLIMAOS_SMTP_PASSWORD`
- `CLIMAOS_SMTP_FROM`
- `CLIMAOS_SMTP_FROM_NAME`
- `CLIMAOS_SMTP_SSL`

Exemplu:

```bash
export CLIMAOS_SMTP_HOST="smtp.gmail.com"
export CLIMAOS_SMTP_PORT="587"
export CLIMAOS_SMTP_USERNAME="noreply@climaos.com"
export CLIMAOS_SMTP_PASSWORD="parola-aplicatie"
export CLIMAOS_SMTP_FROM="noreply@climaos.com"
export CLIMAOS_SMTP_FROM_NAME="ClimaOS"
export CLIMAOS_SMTP_SSL="true"
```

Dacă setările SMTP lipsesc, aplicația folosește un fallback de dezvoltare și scrie codul în log-ul de debug.

## Configurare resetare parolă

Poți ajusta și:

- `CLIMAOS_RESET_CODE_LIFETIME_MINUTES`
- `CLIMAOS_RESET_REQUEST_COOLDOWN_SECONDS`
