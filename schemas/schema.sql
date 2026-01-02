CREATE TABLE districts (
    id uuid NOT NULL,
    name text NOT NULL,
    CONSTRAINT pk_districts PRIMARY KEY (id)
);


CREATE TABLE school_types (
    id uuid NOT NULL,
    name text NOT NULL,
    CONSTRAINT pk_school_types PRIMARY KEY (id)
);


CREATE TABLE schools (
    id uuid NOT NULL,
    district_id uuid NOT NULL,
    name text NOT NULL,
    school_type_id uuid NOT NULL,
    CONSTRAINT pk_schools PRIMARY KEY (id, district_id),
    CONSTRAINT ak_schools_district_id_id UNIQUE (district_id, id),
    CONSTRAINT fk_schools_districts_district_id FOREIGN KEY (district_id) REFERENCES districts (id) ON DELETE CASCADE
);


CREATE TABLE students (
    id uuid NOT NULL,
    district_id uuid NOT NULL,
    name text NOT NULL,
    CONSTRAINT pk_students PRIMARY KEY (district_id, id),
    CONSTRAINT fk_students_districts_district_id FOREIGN KEY (district_id) REFERENCES districts (id) ON DELETE CASCADE,
    CONSTRAINT fk_students_schools_district_id_id FOREIGN KEY (district_id, id) REFERENCES schools (district_id, id) ON DELETE CASCADE
);


CREATE TABLE teachers (
    id uuid NOT NULL,
    district_id uuid NOT NULL,
    name text NOT NULL,
    school_id uuid NOT NULL,
    CONSTRAINT pk_teachers PRIMARY KEY (district_id, id),
    CONSTRAINT fk_teachers_districts_district_id FOREIGN KEY (district_id) REFERENCES districts (id) ON DELETE CASCADE,
    CONSTRAINT fk_teachers_schools_district_id_school_id FOREIGN KEY (district_id, school_id) REFERENCES schools (district_id, id) ON DELETE CASCADE
);


CREATE INDEX ix_teachers_district_id_school_id ON teachers (district_id, school_id);


