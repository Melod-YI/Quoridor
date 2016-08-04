package model.service;

import model.state.GameResultState;
import model.state.GameState;

public interface GameModelService {
	/**
	 * ��ʼ��Ϸ
	 * @return
	 */
	public boolean startGame();
	
	/**
	 * ������Ϸ
	 * @param result ���״̬
	 * @param time ��Ϸʱ��
	 * @return
	 */
	public boolean gameOver(GameResultState result);
	
	/**
	 * ���ʤ��
	 */
    public GameState getGameState();
}
