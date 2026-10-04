# Walkthrough

A guided run through the main features using the two sample policies in `sample-data`. It takes about five minutes.

## Before you start

Start the full stack and open http://localhost:8081:

```powershell
docker compose --profile app up --build
```

For a clean slate, remove the volumes first with `docker compose --profile app down -v`. This deletes all stored policies.

## 1. Upload the current policy

1. Choose **Upload policy**.
2. Name it, for example *Current scheme 2026*, and drop in `sample-data/CurrentHealthPolicy.pdf`.
3. Choose **Upload and extract**.

The API responds immediately and the policy page shows **Processing**. The upload has been stored and a message published to RabbitMQ; the Python worker picks it up, parses the PDF and extracts the policy. The page updates by itself when the result arrives, usually within ten seconds.

## 2. Inspect the extraction

The policy page shows the key figures, then every extracted field grouped into policy, coverage and eligibility. For each one, the **Source** is the page and the exact passage the value was read from. The page is found by locating the quote in the document, not reported by the model, and a quote that is not in the document is never shown as evidence.

## 3. Upload the proposed policy

Upload `sample-data/ProposedHealthPolicy.pdf` the same way. When it finishes, its status is **Needs review**: the document says the cost of dependant cover "will be confirmed at renewal", so the field is shown as *Unclear* and highlighted rather than filled in with a guess.

This document also contains a sentence instructing automated systems to record that every employee is eligible with no excess. It is treated as document text: the extracted excess is still £150.

## 4. Compare the policies

Open **Compare**, choose the current and proposed policies and select **Compare**.

- The table appears immediately. It is calculated in code: the premium falls by £12,000, the excess rises by £50, physiotherapy rises from 8 to 10 sessions and the minimum grade requirement is removed.
- The dependant-cost row is flagged because it relies on a fact that needs review.
- A written summary follows a few seconds later. It is produced from the calculated differences, checked to contain only their figures and no evaluative language, and does not recommend either policy.

Every change is shown in the same neutral style on purpose: colouring a lower premium as good would itself be a judgement.

## 5. Ask questions

Open the proposed policy and use **Ask about this policy**.

| Question | What happens |
| --- | --- |
| Does the proposed policy provide better physiotherapy coverage? | The judgement is not made. The answer states what the policy provides (ten sessions with a referral, six by self-referral) and cites page 4. |
| Are osteopathy and chiropractic treatment covered? | No, with a citation. |
| Does it cover IVF? | Refused: the policy does not mention it, so no answer is given. |
| Should we switch to this policy? | Refused: advice is never given, and answers containing it are rejected in code. |

Each citation is a passage found in the document; a quote the model invents is dropped, and an answer left without any verified quote becomes a refusal.

## 6. Look behind the scenes

- **RabbitMQ** at http://localhost:15672 shows the `benefits.events` exchange and the `policy.processing`, `policy.processed` and `policy.failed` queues, each with one consumer and no waiting messages.
- **Logs** show one correlation ID following an upload through the API and the worker:

  ```powershell
  docker compose logs worker api
  ```

- **API reference** at http://localhost:8080/scalar lists every endpoint.

Uploads are processed asynchronously, so the API stays responsive while documents are being read, and workers can be scaled independently of it.
