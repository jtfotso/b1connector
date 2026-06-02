# B1Connector — What It Is and How It Works

**B1Connector** is a piece of software that acts like a bridge between your online store (e.g., Shopify) and your business management system (SAP Business One, or SAP B1). Its job is simple: whenever something happens in your store — like a new order — B1Connector makes sure that information flows into SAP B1 automatically, correctly, and securely.

---

## The Problem It Solves

Without B1Connector, a new order on your Shopify store has to be manually entered into SAP B1 by a person. That means:

- **Time wasted** on data entry
- **Human error** — typos, missed orders, wrong quantities
- **Delays** — orders sit in Shopify until someone gets around to entering them
- **No unified view** — your store and your ERP don't talk to each other

B1Connector eliminates all of this by automating the process end to end.

---

## How It Works (In Plain English)

Think of B1Connector as a post office for your business data:

```
  Shopify Store           B1Connector            SAP Business One
  ─────────────           ───────────            ────────────────
       │                       │                        │
       │  "New order!"         │                        │
       │ ───────────────────►  │                        │
       │                       │  Translates & sends     │
       │                       │ ──────────────────────► │
       │                       │                        │  Sales order
       │                       │                        │  created
       │                       │  "Done!"               │
       │                       │ ◄────────────────────── │
```

### Step by Step

1. **An order is placed** on your Shopify store.
2. Shopify sends a notification (called a *webhook*) to B1Connector saying, "Hey, there's a new order."
3. B1Connector checks that the notification is legitimate (not fake or malicious) by verifying a digital signature.
4. It looks up which company this store belongs to (B1Connector can handle **multiple stores** at once — each store's data is kept separate and secure).
5. It translates the Shopify order into the format that SAP B1 understands.
6. It sends the translated order to SAP B1, where a sales order is created automatically.
7. The whole process is logged so you can check on it any time.

All of this happens in seconds — no human involvement needed.

---

## Key Features

### 🔌 Multi-Store Support (Multi-Tenant)

B1Connector can serve many different Shopify stores at the same time. Each store has its own isolated set of:
- Connection settings for SAP B1
- Encryption keys (passwords are never stored in plain text)
- Job history and logs

This means one installation of B1Connector can handle your entire portfolio of stores.

### 🔒 Security & Encryption

All sensitive information — Shopify API keys, webhook secrets, SAP B1 passwords — is encrypted before being stored. Nobody can read these credentials even if they gain access to the database.

### 📊 Live Dashboard

B1Connector includes a web-based dashboard where you can:

- See all sync jobs — which succeeded, which failed, which are still waiting
- View per-store statistics (total orders processed, failure rates, etc.)
- Inspect detailed logs for any job to understand what happened
- Add new stores/tenants through a simple form

The dashboard has two views:
- **Admin view**: See everything across all stores
- **Client view**: See only your own store's data

### 🔄 Automatic Retry

If something goes wrong — for example, SAP B1 is temporarily down — B1Connector will automatically retry the job. Failed jobs are recorded with detailed error logs so you can diagnose issues.

### 🧩 Ready for More Platforms

While Shopify is the first connector built, B1Connector is designed so that new platforms (like WooCommerce, Magento, or other e-commerce systems) can be added without rebuilding the entire system.

---

## Who Is This For?

| Role | What B1Connector does for you |
|------|-------------------------------|
| **Business Owner** | Save hours of manual data entry. Orders flow straight from your store into your ERP. |
| **Operations Manager** | No more reconciliation headaches — what's in Shopify matches what's in SAP B1. |
| **Accountant** | Sales orders appear in SAP B1 automatically, so your financial reports are always up to date. |
| **IT Manager** | One centralized system to manage all store-to-ERP connections, with full logging and monitoring. |

---

## The Dashboard — At a Glance

When you log into the B1Connector dashboard, you'll see:

- **Stats cards** showing total jobs, how many completed successfully, how many failed, and how many are pending or in progress
- **A job table** listing every sync job with its status, timestamp, and a "View Logs" button for details
- **For admins**: a tenant overview table showing every connected store and its health

The dashboard refreshes with one click and lets you page through job history.

---

## Behind the Scenes (For the Curious)

B1Connector is built as a background service that runs on a server. It:
- Listens for incoming webhooks from Shopify 24/7
- Stores jobs in a database queue
- Processes them one at a time using a background worker
- Connects to SAP B1's Service Layer API to create sales orders
- Maintains a full audit trail of every action

It can also run in a **mock mode** for testing, where it simulates SAP B1 responses without needing a real SAP B1 server — useful for development and demos.

---

## In Summary

B1Connector is the missing link between your e-commerce platform and your ERP. It replaces manual data entry with an automated, secure, and auditable pipeline. Whether you run one Shopify store or ten, B1Connector keeps your systems in sync so your team can focus on growing the business instead of typing in orders.
