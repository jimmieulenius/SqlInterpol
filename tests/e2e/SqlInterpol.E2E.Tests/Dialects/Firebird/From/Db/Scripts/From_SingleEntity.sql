CREATE TABLE "OrderLine" (
    "OrderId" INT NOT NULL,
    "ProductItemNumber" INT NOT NULL,
    "Quantity" INT NOT NULL,
    "Price" DECIMAL(10, 2) NOT NULL,
    "ProductId" INT NOT NULL
);

INSERT INTO "OrderLine" ("OrderId", "ProductItemNumber", "Quantity", "Price", "ProductId") VALUES (1, 100, 5, 9.99, 200);
INSERT INTO "OrderLine" ("OrderId", "ProductItemNumber", "Quantity", "Price", "ProductId") VALUES (2, 101, 10, 14.99, 201);
INSERT INTO "OrderLine" ("OrderId", "ProductItemNumber", "Quantity", "Price", "ProductId") VALUES (3, 102, 1, 99.99, 202);