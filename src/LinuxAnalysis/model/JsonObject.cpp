#include "JsonObject.h"

JsonObject::JsonObject() {
}


JsonObject::~JsonObject() {
}

string JsonObject::fromBool(bool b) {
	return b ? "true" : "false";
}

string JsonObject::fromInt(int i) {
	return to_string(i);
}

string JsonObject::fromUnsLong(unsigned long l) {
	return to_string(l);
}

string JsonObject::fromString(string s) {
	return "\"" + s + "\"";
}

string JsonObject::fromString(string* s, string def) {
	if (s == NULL) {
		if (def.compare("null") == 0) {
			return def;
		}
		else {
			return "\"" + def + "\"";
		}
	}
	return "\"" + *s + "\"";
}