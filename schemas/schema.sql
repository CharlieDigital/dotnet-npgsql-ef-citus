CREATE TABLE dealerships (
    id uuid NOT NULL,
    name text NOT NULL,
    brand text NOT NULL,
    CONSTRAINT pk_dealerships PRIMARY KEY (id)
);


CREATE TABLE vehicles (
    id uuid NOT NULL,
    dealership_id uuid NOT NULL DEFAULT ((get_tenant()::uuid)),
    vin text NOT NULL,
    stock_number text NOT NULL,
    model text NOT NULL,
    year text NOT NULL,
    used boolean NOT NULL,
    CONSTRAINT pk_vehicles PRIMARY KEY (dealership_id, id)
);


CREATE TABLE customers (
    id uuid NOT NULL,
    dealership_id uuid NOT NULL,
    vehicle_id uuid,
    first_name text NOT NULL,
    last_name text NOT NULL,
    email text NOT NULL,
    CONSTRAINT pk_customers PRIMARY KEY (dealership_id, id),
    CONSTRAINT fk_customers_vehicles_dealership_id_vehicle_id FOREIGN KEY (dealership_id, vehicle_id) REFERENCES vehicles (dealership_id, id) ON DELETE SET NULL
);


CREATE TABLE parts_orders (
    id uuid NOT NULL,
    dealership_id uuid NOT NULL,
    vehicle_id uuid,
    part_number text NOT NULL,
    description text NOT NULL,
    quantity integer NOT NULL,
    CONSTRAINT pk_parts_orders PRIMARY KEY (dealership_id, id),
    CONSTRAINT fk_parts_orders_vehicles_dealership_id_vehicle_id FOREIGN KEY (dealership_id, vehicle_id) REFERENCES vehicles (dealership_id, id)
);


CREATE TABLE service_records (
    id uuid NOT NULL,
    dealership_id uuid NOT NULL DEFAULT ((get_tenant()::uuid)),
    vehicle_id uuid NOT NULL,
    serviced_on_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_service_records PRIMARY KEY (dealership_id, id),
    CONSTRAINT fk_service_records_dealerships_dealership_id FOREIGN KEY (dealership_id) REFERENCES dealerships (id) ON DELETE CASCADE,
    CONSTRAINT fk_service_records_vehicles_dealership_id_vehicle_id FOREIGN KEY (dealership_id, vehicle_id) REFERENCES vehicles (dealership_id, id) ON DELETE CASCADE
);


CREATE INDEX ix_customers_dealership_id_vehicle_id ON customers (dealership_id, vehicle_id);


CREATE INDEX ix_parts_orders_dealership_id_vehicle_id ON parts_orders (dealership_id, vehicle_id);


CREATE INDEX ix_service_records_dealership_id_vehicle_id ON service_records (dealership_id, vehicle_id);


CREATE UNIQUE INDEX ix_vehicles_dealership_id_vin ON vehicles (dealership_id, vin);


