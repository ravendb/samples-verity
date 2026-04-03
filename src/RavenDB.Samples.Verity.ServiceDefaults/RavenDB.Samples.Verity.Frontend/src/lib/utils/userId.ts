import { generateFunEmojiAvatar } from './avatar';

const USER_ID_KEY = 'library_user_id';

export interface UserIdInfo {
	userId: string;
	isNewUser: boolean;
}

export function getUserId(): string {
	return getUserIdWithInfo().userId;
}

export function getUserIdWithInfo(): UserIdInfo {
	if (typeof localStorage === 'undefined') {
		return {
			userId: generateUUID(),
			isNewUser: true
		};
	}

	let userId = localStorage.getItem(USER_ID_KEY);
	const isNewUser = !userId;
	if (!userId) {
		userId = generateUUID();
		localStorage.setItem(USER_ID_KEY, userId);
	}
	return {
		userId,
		isNewUser
	};
}

function generateUUID(): string {
	return crypto.randomUUID();
}

export function getUserAvatarUrl(userId: string): string {
	return generateFunEmojiAvatar(userId);
}
