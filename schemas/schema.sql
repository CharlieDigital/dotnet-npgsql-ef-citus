CREATE TABLE districts (
    id uuid NOT NULL,
    name text NOT NULL,
    CONSTRAINT pk_districts PRIMARY KEY (id)
);


CREATE TABLE schools (
    id uuid NOT NULL,
    district_id uuid NOT NULL,
    name text NOT NULL,
    CONSTRAINT pk_schools PRIMARY KEY (id, district_id),
    CONSTRAINT fk_schools_districts_district_id FOREIGN KEY (district_id) REFERENCES districts (id) ON DELETE CASCADE
);


CREATE TABLE students (
    id uuid NOT NULL,
    district_id uuid NOT NULL,
    name text NOT NULL,
    CONSTRAINT pk_students PRIMARY KEY (id, district_id),
    CONSTRAINT fk_students_districts_district_id FOREIGN KEY (district_id) REFERENCES districts (id) ON DELETE CASCADE,
    CONSTRAINT fk_students_schools_id_district_id FOREIGN KEY (id, district_id) REFERENCES schools (id, district_id) ON DELETE CASCADE
);


CREATE TABLE teachers (
    id uuid NOT NULL,
    district_id uuid NOT NULL,
    name text NOT NULL,
    CONSTRAINT pk_teachers PRIMARY KEY (id, district_id),
    CONSTRAINT fk_teachers_districts_district_id FOREIGN KEY (district_id) REFERENCES districts (id) ON DELETE CASCADE,
    CONSTRAINT fk_teachers_schools_id_district_id FOREIGN KEY (id, district_id) REFERENCES schools (id, district_id) ON DELETE CASCADE
);


CREATE INDEX ix_schools_district_id ON schools (district_id);


CREATE INDEX ix_students_district_id ON students (district_id);


CREATE INDEX ix_teachers_district_id ON teachers (district_id);


