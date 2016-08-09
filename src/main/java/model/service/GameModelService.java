package model.service;

import model.state.GameResultState;
import model.state.GameState;

public interface GameModelService {
	/**
	 * 开始游戏
	 * @return
	 */
	public boolean startGame();
	
	/**
	 * 结束游戏
	 * @param result 游戏状态״̬
	 * @param time 游戏时间
	 * @return
	 */
	public boolean gameOver(GameResultState result);
	
	/**
	 * 获得胜负
	 */
    public GameState getGameState();
}
