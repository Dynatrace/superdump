#include "FileExtractor.h"

#include <archive.h>
#include <archive_entry.h>
#include <string>

using namespace std;

bool extractArchive(string targetDirectory, string filepath);
bool extractGz(string targetDirectory, string filepath);
int copy_data(struct archive *ar, struct archive *aw);

FileExtractor::FileExtractor()
{
}


FileExtractor::~FileExtractor()
{
}

bool FileExtractor::extractFile(string targetDirectory, string filepath) {
	if (extractArchive(targetDirectory, filepath) == false) {
		return extractGz(targetDirectory, filepath);
	}
	return true;
}

bool extractArchive(string targetDirectory, string filepath) {
	struct archive *a;
	struct archive *ext;
	struct archive_entry *entry;
	int flags;
	int r;

	/* Select which attributes we want to restore. */
	flags = ARCHIVE_EXTRACT_TIME;
	flags |= ARCHIVE_EXTRACT_PERM;
	flags |= ARCHIVE_EXTRACT_ACL;
	flags |= ARCHIVE_EXTRACT_FFLAGS;

	a = archive_read_new();
	archive_read_support_filter_all(a);
	archive_read_support_format_all(a);
	archive_read_support_compression_all(a);
	ext = archive_write_disk_new();
	archive_write_disk_set_options(ext, flags);
	archive_write_disk_set_standard_lookup(ext);
	if ((r = archive_read_open_filename(a, filepath.c_str(), 10240)))
		return false;
	for (;;) {
		r = archive_read_next_header(a, &entry);
		if (r == ARCHIVE_EOF)
			break;
		if (r < ARCHIVE_OK)
			fprintf(stderr, "%s\n", archive_error_string(a));
		if (r < ARCHIVE_WARN)
			return false;

		string pathname = targetDirectory + "/" + archive_entry_pathname(entry);
		archive_entry_set_pathname(entry, pathname.c_str());
		r = archive_write_header(ext, entry);
		if (r < ARCHIVE_OK)
			fprintf(stderr, "%s\n", archive_error_string(ext));
		else if (archive_entry_size(entry) > 0) {
			r = copy_data(a, ext);
			if (r < ARCHIVE_OK)
				fprintf(stderr, "%s\n", archive_error_string(ext));
			if (r < ARCHIVE_WARN)
				return false;
		}
		r = archive_write_finish_entry(ext);
		if (r < ARCHIVE_OK)
			fprintf(stderr, "%s\n", archive_error_string(ext));
		if (r < ARCHIVE_WARN)
			return false;
	}
	archive_read_close(a);
	archive_read_free(a);
	archive_write_close(ext);
	archive_write_free(ext);
	return true;
}

int copy_data(struct archive *ar, struct archive *aw) {
	int r;
	const void *buff;
	size_t size;
	int64_t offset;

	for (;;) {
		r = archive_read_data_block(ar, &buff, &size, &offset);
		if (r == ARCHIVE_EOF)
			return (ARCHIVE_OK);
		if (r < ARCHIVE_OK)
			return (r);
		r = archive_write_data_block(aw, buff, size, offset);
		if (r < ARCHIVE_OK) {
			fprintf(stderr, "%s\n", archive_error_string(aw));
			return (r);
		}
	}
}

bool extractGz(string targetDirectory, string filepath) {
	struct archive *a;
	struct archive *ext;
	struct archive_entry *entry;
	int flags;
	int r;

	/* Select which attributes we want to restore. */
	flags = ARCHIVE_EXTRACT_TIME;
	flags |= ARCHIVE_EXTRACT_PERM;
	flags |= ARCHIVE_EXTRACT_ACL;
	flags |= ARCHIVE_EXTRACT_FFLAGS;

	a = archive_read_new();
	archive_read_support_filter_gzip(a);
	archive_read_support_format_raw(a);
	
	if ((r = archive_read_open_filename(a, filepath.c_str(), 10240))) {
		return false;
	}

	string pathname;
	int len = filepath.find_last_of('.');
	if (len > 0) {
		pathname = filepath.substr(0, len);
	}
	else {
		printf("Failed to detect file format of %s!\r\n", filepath.c_str());
		pathname = filepath;
	}

	int size;
	char buf[1024];
	FILE *fp = fopen(pathname.c_str(), "wb+");

	while(true) {
		r = archive_read_next_header(a, &entry);
		if (r == ARCHIVE_EOF)
			break;
		if (r < ARCHIVE_OK)
			fprintf(stderr, "%s\n", archive_error_string(a));
		if (r < ARCHIVE_WARN)
			return false;

		while ((size = archive_read_data(a, buf, 1024)) > 0) {
			fwrite(buf, sizeof(char), size, fp);
		}
		archive_read_data_skip(a);
	}
	fclose(fp);
	archive_read_free(a);
	return true;
}