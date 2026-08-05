# Public Registration Specification

## Purpose

Allow an anonymous runner to self-register for a race through a public `RegistrationLink`, submit required personal and payment-reference data, upload transfer proof, and progress through the `Registration` state machine (`Pendiente -> ComprobanteSubido -> Confirmada | Rechazada`) without an account or authentication.

## Requirements

### Requirement: Public Link Access Validation

The system MUST allow anonymous access to the registration form only through a valid, non-revoked, non-expired `RegistrationLink` for a race whose "inscripciones abiertas" flag is enabled.

#### Scenario: Valid open link
- GIVEN a `RegistrationLink` that is not revoked and not expired, for a race with registration open
- WHEN an anonymous user opens the link
- THEN the system MUST return the race and category data needed to register

#### Scenario: Revoked or expired link rejected
- GIVEN a `RegistrationLink` that is revoked or past its expiration
- WHEN an anonymous user opens the link
- THEN the system MUST reject access without exposing registration data

#### Scenario: Registration not open
- GIVEN a valid link for a race whose "inscripciones abiertas" flag is disabled
- WHEN an anonymous user opens the link
- THEN the system MUST reject access

### Requirement: Anonymous Registration Submission

The system MUST accept anonymous submissions creating a `Registration` in state `Pendiente`, requiring runner identity data, a selected `RaceCategory`, and `FechaNacimiento`, without requiring authentication.

#### Scenario: Valid submission accepted
- GIVEN a valid open `RegistrationLink` and a submission before the race deadline
- WHEN an anonymous user submits required registration data
- THEN the system MUST create a `Registration` in state `Pendiente`

#### Scenario: Deadline passed
- GIVEN a valid link whose race registration deadline has passed
- WHEN an anonymous user submits registration data
- THEN the system MUST reject the submission

#### Scenario: Submission not capacity-gated
- GIVEN a race category at or above its configured capacity
- WHEN an anonymous user submits a valid registration before the deadline
- THEN the system MUST accept the submission (capacity is enforced only at confirm)

### Requirement: Date of Birth Requirement

`FechaNacimiento` MUST be a required field on every registration submission and MUST be a plausible past date.

#### Scenario: Missing date of birth
- GIVEN a submission without `FechaNacimiento`
- WHEN the submission is processed
- THEN the system MUST reject it

#### Scenario: Future or implausible date of birth
- GIVEN a submission with `FechaNacimiento` in the future or older than a realistic human lifespan
- WHEN the submission is processed
- THEN the system MUST reject it

### Requirement: Minor Emergency Contact Requirement

The system MUST compute the runner's age at the race date from `Registration.FechaNacimiento` and, when that age is below the configured `RegistrationOptions:EdadMayoriaEdad` threshold, MUST require and validate emergency-contact fields on the same submission.

#### Scenario: Minor without emergency contact rejected
- GIVEN a submission whose computed age at race date is below the configured minority threshold
- AND no emergency-contact fields are provided
- WHEN the submission is processed
- THEN the system MUST reject the submission

#### Scenario: Minor with emergency contact accepted
- GIVEN a submission whose computed age at race date is below the configured minority threshold
- AND valid emergency-contact fields are provided
- WHEN the submission is processed
- THEN the system MUST create the `Registration` in state `Pendiente`

#### Scenario: Adult submission does not require emergency contact
- GIVEN a submission whose computed age at race date meets or exceeds the configured threshold
- WHEN the submission is processed
- THEN the system MUST NOT require emergency-contact fields

### Requirement: Receipt Upload and State Transition

The system MUST allow the anonymous submitter to attach a `ReferenciaTransferencia` and payment receipt to a `Registration` in state `Pendiente`, transitioning it to `ComprobanteSubido`.

#### Scenario: Receipt upload transitions state
- GIVEN a `Registration` in state `Pendiente`
- WHEN the submitter uploads a receipt with `ReferenciaTransferencia`
- THEN the system MUST transition the `Registration` to state `ComprobanteSubido`

#### Scenario: Upload rejected outside Pendiente state
- GIVEN a `Registration` already in state `ComprobanteSubido`, `Confirmada`, or `Rechazada`
- WHEN a receipt upload is attempted
- THEN the system MUST reject the upload

### Requirement: Public Endpoint Rate Limiting

Anonymous submission and upload endpoints MUST be protected by the `RateLimiting:PublicRegistration` policy.

#### Scenario: Rate limit exceeded
- GIVEN a client that exceeds the configured `RateLimiting:PublicRegistration` request rate
- WHEN the client submits an additional anonymous registration request
- THEN the system MUST reject the request with a rate-limit response

## Key Learnings

1. The proposal's Decision #5 requires a real `FechaNacimiento` field on `Registration`, not a category-range age inference.
2. Capacity is enforced only at confirmation time, never at anonymous submission, per the proposal's explicit non-goal boundary.
3. The registration state machine has exactly four states with one linear happy path and two terminal branches from `ComprobanteSubido`.
