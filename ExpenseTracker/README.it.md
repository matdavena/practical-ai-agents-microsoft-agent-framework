# Expense Tracker

> Un'applicazione completa di gestione spese alimentata da AI che dimostra tutti i concetti del Microsoft Agent Framework

[![.NET](https://img.shields.io/badge/.NET-10.0-purple)](https://dotnet.microsoft.com/)
[![License](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)

## Panoramica

Expense Tracker è un'applicazione multi-interfaccia per la gestione delle spese personali, alimentata da agenti AI. Permette agli utenti di registrare spese tramite linguaggio naturale o foto di scontrini, con categorizzazione automatica e query conversazionali.

Questo progetto funge da **progetto finale** che dimostra tutti i concetti trattati nel corso Learning Agent Framework:

- Tools e Function Calling
- Structured Output
- Vision AI (Parsing Scontrini)
- Orchestrazione Multi-Agent
- RAG con Vector Stores
- Gestione Budget e Avvisi
- Deploy multi-piattaforma (Console, Telegram, Web API)

## Architettura

```
┌─────────────────────────────────────────────────────────────────────┐
│                         LIVELLO PRESENTAZIONE                        │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │   Console    │  │   Telegram   │  │   Web API    │              │
│  │     App      │  │     Bot      │  │              │              │
│  └──────┬───────┘  └──────┬───────┘  └──────┬───────┘              │
└─────────┼─────────────────┼─────────────────┼───────────────────────┘
          │                 │                 │
          └─────────────────┴────────┬────────┘
                                     ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      EXPENSE TRACKER CORE                            │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                    AGENTI                                      │  │
│  │  ┌─────────────────┐                                          │  │
│  │  │   Orchestrator  │ ─── Analizza intent, delega agli agenti  │  │
│  │  │     Agent       │                                          │  │
│  │  └────────┬────────┘                                          │  │
│  │           │                                                    │  │
│  │     ┌─────┴─────┬─────────────┐                               │  │
│  │     ▼           ▼             ▼                               │  │
│  │  ┌──────┐  ┌──────────┐  ┌──────────┐                        │  │
│  │  │Parser│  │ Receipt  │  │  Budget  │                        │  │
│  │  │Agent │  │  Agent   │  │  Tools   │                        │  │
│  │  └──────┘  └──────────┘  └──────────┘                        │  │
│  │  (Testo)   (Visione)     (Avvisi)                            │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                         TOOLS                                  │  │
│  │  AddExpense │ GetExpenses │ GetCategories │ SearchExpenses    │  │
│  │  SetBudget │ GetBudgetStatus │ GetBudgetAlerts                │  │
│  └───────────────────────────────────────────────────────────────┘  │
│                                                                      │
│  ┌───────────────────────────────────────────────────────────────┐  │
│  │                       SERVIZI                                  │  │
│  │  IExpenseService │ ICategoryService │ IBudgetService          │  │
│  └───────────────────────────────────────────────────────────────┘  │
└─────────────────────────────────────────────────────────────────────┘
                                     │
                                     ▼
┌─────────────────────────────────────────────────────────────────────┐
│                      LIVELLO INFRASTRUTTURA                          │
│  ┌──────────────┐  ┌──────────────┐  ┌──────────────┐              │
│  │    SQLite    │  │    Qdrant    │  │    OpenAI    │              │
│  │  (Spese,     │  │  (Ricerca    │  │  (LLM +      │              │
│  │   Budget)    │  │   Semantica) │  │   Visione)   │              │
│  └──────────────┘  └──────────────┘  └──────────────┘              │
└─────────────────────────────────────────────────────────────────────┘
```

## Funzionalita'

| Funzionalita' | Descrizione |
|---------------|-------------|
| Input in Linguaggio Naturale | "Ho speso 45 euro al supermercato" |
| Scansione Scontrini | Vision AI estrae dati dalle foto degli scontrini |
| Categorizzazione Automatica | L'AI determina la categoria |
| Gestione Budget | Imposta limiti per categoria o globali |
| Avvisi Budget | Warning all'80%, superato, critico |
| Ricerca Semantica | Trova spese simili con Qdrant |
| Multi-Piattaforma | Console, Bot Telegram, REST API |

## Avvio Rapido

### Prerequisiti

- .NET 10 SDK
- Chiave API OpenAI
- Docker (opzionale, per ricerca semantica con Qdrant)
- Token Bot Telegram (opzionale, per il bot Telegram)

### Variabili d'Ambiente

| Variabile | Descrizione | Richiesta |
|-----------|-------------|-----------|
| `OPENAI_API_KEY` | Chiave API OpenAI | Si |
| `OPENAI_MODEL` | Modello da usare (default: gpt-4o-mini) | No |
| `TELEGRAM_BOT_TOKEN` | Token del bot Telegram | Solo per Telegram |

### Esecuzione App Console

```bash
cd ExpenseTracker/src/ExpenseTracker.Console
dotnet run
```

### Esecuzione Bot Telegram

```bash
# Imposta token Telegram
$env:TELEGRAM_BOT_TOKEN = "il-tuo-token"

cd ExpenseTracker/src/ExpenseTracker.Telegram
dotnet run
```

### Esecuzione Web API

```bash
cd ExpenseTracker/src/ExpenseTracker.Api
dotnet run
```

Swagger UI disponibile su: http://localhost:5000

### Avvio Qdrant (per Ricerca Semantica)

```bash
cd ExpenseTracker
docker compose up -d
```

## Categorie Predefinite

| ID | Nome | Icona |
|----|------|-------|
| food | Alimentari | :shopping_cart: |
| restaurant | Ristorante | :fork_and_knife: |
| transport | Trasporti | :car: |
| fuel | Carburante | :fuelpump: |
| health | Salute | :pill: |
| entertainment | Intrattenimento | :clapper: |
| shopping | Shopping | :shopping_bags: |
| bills | Bollette | :page_facing_up: |
| home | Casa | :house: |
| other | Altro | :package: |

## Comandi Telegram

| Comando | Descrizione |
|---------|-------------|
| `/start` | Messaggio di benvenuto e registrazione |
| `/help` | Istruzioni d'uso |
| `/report` | Riepilogo spese mensili |
| `/budget` | Stato budget |
| `/categories` | Lista categorie disponibili |
| `/reset` | Reset conversazione |

## Esempi di Interazione

### Console / Telegram / Chat API

```
Utente: Ho speso 45 euro al supermercato
AI: [chiama add_expense] Spesa salvata! 45,00 EUR per Alimentari.

Utente: Quanto ho speso questo mese?
AI: [chiama get_category_summary]
    Riepilogo Dicembre 2024:
    Alimentari: 245,50 EUR (12 spese)
    Ristorante: 89,00 EUR (3 spese)
    Carburante: 60,00 EUR (2 spese)
    Totale: 394,50 EUR

Utente: Imposta un budget di 500 euro al mese
AI: [chiama set_budget] Budget impostato! Limite mensile di 500,00 EUR (globale).

Utente: Sono nel budget?
AI: [chiama get_budget_status]
    Ti rimangono 105,50 EUR (79% utilizzato).
```

### Foto Scontrino (Telegram)

```
Utente: [invia foto scontrino]
AI: Scontrino analizzato:
    Importo: 32,50 euro
    Spesa supermercato
    Categoria: food
    Data: 2024-12-24

    Vuoi salvare questa spesa?
    [Salva] [Annulla]
```

## Stack Tecnologico

| Componente | Tecnologia |
|------------|------------|
| Framework | .NET 10 |
| Framework AI | Microsoft Agent Framework |
| LLM | OpenAI GPT-4o (testo + visione) |
| Database | SQLite + Dapper |
| Vector Store | Qdrant |
| Telegram | Telegram.Bot |
| Web API | ASP.NET Core |
| Console UI | Spectre.Console |

## Fasi di Implementazione

| Fase | Descrizione | Stato |
|------|-------------|-------|
| 1 | Domain e Database | Completato |
| 2 | Expense Parser Agent (Structured Output) | Completato |
| 3 | Tools e Orchestrator Agent | Completato |
| 4 | Vision AI (Parsing Scontrini) | Completato |
| 5 | Orchestrazione Multi-Agent | Completato |
| 6 | Bot Telegram | Completato |
| 7 | Web API | Completato |
| 8 | Ricerca Semantica (RAG + Qdrant) | Completato |
| 9 | Budget e Avvisi | Completato |
| 10 | Documentazione | Completato |

## Risorse Correlate

- [Microsoft Agent Framework](https://github.com/microsoft/agents)
- [OpenAI Function Calling](https://platform.openai.com/docs/guides/function-calling)
- [OpenAI Vision](https://platform.openai.com/docs/guides/vision)
- [Qdrant Vector Database](https://qdrant.tech/)
- [Telegram Bot API](https://core.telegram.org/bots/api)

---

**Questo e' un progetto finale che dimostra tutti i concetti del Microsoft Agent Framework in un'applicazione reale.**
