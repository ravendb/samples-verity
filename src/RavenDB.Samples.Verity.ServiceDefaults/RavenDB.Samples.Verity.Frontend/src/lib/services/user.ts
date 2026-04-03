import { callApi } from '$lib/api';

export interface BorrowedBook {
	id: string;
	bookId: string;
	title: string;
	overdue: boolean;
}

export interface UserProfile {
	id: string;
	borrowed: BorrowedBook[];
}

export interface Notification {
	id: string;
	text: string;
	referencedItemId?: string;
}

export interface NotificationCount {
	count: number;
}

/**
 * Fetches the user profile from the API.
 * @returns The user profile including borrowed books
 */
export async function getUserProfile(): Promise<UserProfile> {
	return callApi<UserProfile>('/user/profile');
}

/**
 * Fetches the user's notifications from the API.
 * @returns An array of notifications
 */
export async function getNotifications(): Promise<Notification[]> {
	return callApi<Notification[]>('/user/notifications');
}

/**
 * Fetches the count of user's notifications from the API.
 * @returns The notification count
 */
export async function getNotificationCount(): Promise<NotificationCount> {
	return callApi<NotificationCount>('/user/notifications/count');
}

/**
 * Deletes a notification by its ID.
 * @param id - The notification ID to delete
 */
export async function deleteNotification(id: string): Promise<void> {
	// Normalize the identifier first
	id = id.replace('Notifications/', '');

	await callApi<void>(`/user/notifications/${id}`, {
		method: 'DELETE'
	});
}

/**
 * Returns a borrowed book by its ID.
 * @param id - The BorrowedBook ID to return
 */
export async function returnBook(id: string): Promise<void> {
	// Normalize the identifier first
	id = id.replace('BorrowedBooks/', '');

	await callApi<void>(`/user/books/${id}/return`, {
		method: 'POST'
	});
}

/**
 * Borrows a book by its ID.
 * @param bookId - The book ID to borrow
 */
export async function borrowBook(bookId: string): Promise<void> {
	await callApi<void>('/user/books', {
		method: 'POST',
		headers: {
			'Content-Type': 'application/json'
		},
		body: JSON.stringify({ bookId })
	});
}
