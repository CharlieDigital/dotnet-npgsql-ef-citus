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


CREATE TABLE service_records (
    id uuid NOT NULL,
    dealership_id uuid NOT NULL DEFAULT ((get_tenant()::uuid)),
    vehicle_id uuid NOT NULL,
    serviced_on_utc timestamp with time zone NOT NULL,
    CONSTRAINT pk_service_records PRIMARY KEY (dealership_id, id),
    CONSTRAINT fk_service_records_dealerships_dealership_id FOREIGN KEY (dealership_id) REFERENCES dealerships (id) ON DELETE CASCADE,
    CONSTRAINT fk_service_records_vehicles_dealership_id_vehicle_id FOREIGN KEY (dealership_id, vehicle_id) REFERENCES vehicles (dealership_id, id) ON DELETE CASCADE
);


CREATE INDEX ix_service_records_dealership_id_vehicle_id ON service_records (dealership_id, vehicle_id);


CREATE UNIQUE INDEX ix_vehicles_dealership_id_vin ON vehicles (dealership_id, vin);


